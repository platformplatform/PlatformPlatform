using Account.Features.Subscriptions.Domain;
using FluentAssertions;
using SharedKernel.Domain;
using Xunit;

namespace Account.Tests.Subscriptions.Domain;

public sealed class SubscriptionTests
{
    [Fact]
    public void AdvanceSubscribedSinceBackwardFromBillingEvent_WhenUnset_ShouldAssignFromEvent()
    {
        // Arrange
        var subscription = Subscription.Create(TenantId.NewId());
        var firstSubscriptionCreatedAt = DateTimeOffset.Parse("2026-01-15T10:00:00Z");

        // Act
        subscription.AdvanceSubscribedSinceBackwardFromBillingEvent(firstSubscriptionCreatedAt);

        // Assert
        subscription.SubscribedSince.Should().Be(firstSubscriptionCreatedAt);
    }

    [Fact]
    public void AdvanceSubscribedSinceBackwardFromBillingEvent_WhenOlderEventArrivesLate_ShouldRewindBackward()
    {
        // subscription has SubscribedSince=T1 from a SubscriptionCreated event; a reconcile then
        // recovers an older SubscriptionCreated event at T0 < T1.
        // Arrange
        var subscription = Subscription.Create(TenantId.NewId());
        var t1 = DateTimeOffset.Parse("2026-01-15T10:00:00Z");
        subscription.AdvanceSubscribedSinceBackwardFromBillingEvent(t1);
        var t0 = DateTimeOffset.Parse("2025-08-01T00:00:00Z");

        // Act
        subscription.AdvanceSubscribedSinceBackwardFromBillingEvent(t0);

        // Assert
        subscription.SubscribedSince.Should().Be(t0);
    }

    [Fact]
    public void AdvanceSubscribedSinceBackwardFromBillingEvent_WhenSameTenantStartsNewSubscriptionLater_ShouldPreserveOriginal()
    {
        // tenant cancels original subscription, then starts a brand-new Stripe subscription on the
        // same tenant. The new SubscriptionCreated event at T2 > T0 must not move SubscribedSince forward.
        // Arrange
        var subscription = Subscription.Create(TenantId.NewId());
        var t0 = DateTimeOffset.Parse("2025-08-01T00:00:00Z");
        subscription.AdvanceSubscribedSinceBackwardFromBillingEvent(t0);
        subscription.ResetToFreePlan();
        var t2 = DateTimeOffset.Parse("2026-04-01T10:00:00Z");

        // Act
        subscription.AdvanceSubscribedSinceBackwardFromBillingEvent(t2);

        // Assert
        subscription.SubscribedSince.Should().Be(t0);
    }

    [Fact]
    public void ResetToFreePlan_WhenCalled_ShouldPreserveSubscribedSince()
    {
        // Arrange
        var subscription = Subscription.Create(TenantId.NewId());
        var t0 = DateTimeOffset.Parse("2026-01-15T10:00:00Z");
        subscription.AdvanceSubscribedSinceBackwardFromBillingEvent(t0);

        // Act
        subscription.ResetToFreePlan();

        // a cancellation must never clear SubscribedSince. Gaps in subscription coverage are
        // irrelevant to the tenant-scoped invariant.
        // Assert
        subscription.SubscribedSince.Should().Be(t0);
    }

    [Fact]
    public void SetStripeSubscription_WhenCalled_ShouldNotTouchSubscribedSince()
    {
        // Arrange
        var subscription = Subscription.Create(TenantId.NewId());
        var now = DateTimeOffset.Parse("2026-01-15T10:00:00Z");

        // Act
        subscription.SetStripeSubscription(new StripeSubscriptionId("sub_test"), SubscriptionPlan.Standard, 149.00m, "DKK", now.AddDays(30), null);

        // SubscribedSince is sourced exclusively from SubscriptionCreated BillingEvents, never from
        // the live Stripe state mutation. The cache stays null until the matching BillingEvent is appended.
        // Assert
        subscription.SubscribedSince.Should().BeNull();
    }
}
