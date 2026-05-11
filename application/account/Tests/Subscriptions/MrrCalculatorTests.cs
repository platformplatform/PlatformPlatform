using Account.Features.Subscriptions.Domain;
using Account.Features.Subscriptions.Shared;
using FluentAssertions;
using SharedKernel.Domain;
using Xunit;

namespace Account.Tests.Subscriptions;

/// <summary>
///     Pure-function tests for <see cref="MrrCalculator.ForwardMrr" />. The calculator funnels both the
///     dashboard KPI sum and the KPI/trend consistency check, so any regression in the cancel /
///     scheduled-downgrade / no-schedule branches silently distorts BLENDED MRR. The ScheduledPlan-set-but-
///     ScheduledPriceAmount-null case mirrors the cancel-then-reschedule edge that motivated the unconditional
///     reconciliation in <c>SyncStateFromStripe</c> and the <c>ScheduledPriceMissing</c> drift discrepancy.
/// </summary>
public sealed class MrrCalculatorTests
{
    [Fact]
    public void ForwardMrr_WhenSubscriptionCancelAtPeriodEnd_ShouldReturnZero()
    {
        // Arrange
        var subscription = Subscription.Create(TenantId.NewId());
        subscription.SetStripeSubscription(null, SubscriptionPlan.Standard, 149m, "DKK", DateTimeOffset.UtcNow.AddDays(30), null);
        subscription.SetCancellation(true, CancellationReason.NoLongerNeeded, null);

        // Act
        var forwardMrr = MrrCalculator.ForwardMrr(subscription);

        // Assert
        forwardMrr.Should().Be(0m, "a subscription cancelling at period end contributes zero to forward MRR");
    }

    [Fact]
    public void ForwardMrr_WhenScheduledDowngradeFromPremiumToStandard_ShouldReturnScheduledPrice()
    {
        // Arrange
        var subscription = Subscription.Create(TenantId.NewId());
        subscription.SetStripeSubscription(null, SubscriptionPlan.Premium, 299m, "DKK", DateTimeOffset.UtcNow.AddDays(30), null);
        subscription.SetScheduledPlan(SubscriptionPlan.Standard, 149m);

        // Act
        var forwardMrr = MrrCalculator.ForwardMrr(subscription);

        // Assert
        forwardMrr.Should().Be(149m, "a scheduled downgrade contributes the scheduled (downgraded) price, not the current price");
    }

    [Fact]
    public void ForwardMrr_WhenNoScheduledChange_ShouldReturnCurrentPrice()
    {
        // Arrange
        var subscription = Subscription.Create(TenantId.NewId());
        subscription.SetStripeSubscription(null, SubscriptionPlan.Standard, 149m, "DKK", DateTimeOffset.UtcNow.AddDays(30), null);

        // Act
        var forwardMrr = MrrCalculator.ForwardMrr(subscription);

        // Assert
        forwardMrr.Should().Be(149m, "without a scheduled change the current price is the forward MRR contribution");
    }

    [Fact]
    public void ForwardMrr_WhenScheduledPlanSetButScheduledPriceAmountNull_ShouldFallThroughToCurrentPrice()
    {
        // Edge case from the cancel-then-reschedule pair landing in a single sync window: ScheduledPlan is set
        // but ScheduledPriceAmount stayed null from an earlier transition. The calculator must not throw; it
        // falls back to the current price. <c>BillingDriftDetector</c> separately surfaces this state via the
        // <c>ScheduledPriceMissing</c> discrepancy, and <c>SyncStateFromStripe</c>'s unconditional
        // reconciliation now prevents the state from persisting beyond a single sync.
        // Arrange
        var subscription = Subscription.Create(TenantId.NewId());
        subscription.SetStripeSubscription(null, SubscriptionPlan.Premium, 299m, "DKK", DateTimeOffset.UtcNow.AddDays(30), null);
        subscription.SetScheduledPlan(SubscriptionPlan.Standard, null);

        // Act
        var forwardMrr = MrrCalculator.ForwardMrr(subscription);

        // Assert
        forwardMrr.Should().Be(299m, "ScheduledPriceAmount null must fall through to CurrentPriceAmount rather than throw");
    }

    [Fact]
    public void ForwardMrr_WhenPlanIsBasisAndNoStripeCustomerId_ShouldReturnZero()
    {
        // A brand-new tenant on the free plan has never been associated with a Stripe customer. CurrentPriceAmount
        // is null, so the calculator short-circuits before the cancel / scheduled-downgrade branches.
        // Arrange
        var subscription = Subscription.Create(TenantId.NewId());

        // Act
        var forwardMrr = MrrCalculator.ForwardMrr(subscription);

        // Assert
        subscription.Plan.Should().Be(SubscriptionPlan.Basis);
        subscription.StripeCustomerId.Should().BeNull();
        forwardMrr.Should().Be(0m, "a Basis-plan subscription with no Stripe customer contributes zero to forward MRR");
    }
}
