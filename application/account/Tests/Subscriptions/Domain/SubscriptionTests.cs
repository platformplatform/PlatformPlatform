using Account.Features.Subscriptions.Domain;
using FluentAssertions;
using SharedKernel.Domain;
using Xunit;

namespace Account.Tests.Subscriptions.Domain;

public sealed class SubscriptionTests
{
    [Fact]
    public void SetStripeSubscription_WhenFirstActivationOnPaidPlan_ShouldCaptureSubscribedSince()
    {
        // Arrange
        var subscription = Subscription.Create(TenantId.NewId());
        var now = DateTimeOffset.Parse("2026-01-15T10:00:00Z");

        // Act
        subscription.SetStripeSubscription(new StripeSubscriptionId("sub_test"), SubscriptionPlan.Standard, 29.99m, "USD", now.AddDays(30), null, now);

        // Assert
        subscription.SubscribedSince.Should().Be(now);
    }

    [Fact]
    public void SetStripeSubscription_WhenActivatingFreePlan_ShouldNotCaptureSubscribedSince()
    {
        // Arrange
        var subscription = Subscription.Create(TenantId.NewId());
        var now = DateTimeOffset.Parse("2026-01-15T10:00:00Z");

        // Act
        subscription.SetStripeSubscription(null, SubscriptionPlan.Basis, null, null, null, null, now);

        // Assert
        subscription.SubscribedSince.Should().BeNull();
    }

    [Fact]
    public void SetStripeSubscription_WhenAlreadyActive_ShouldNotOverwriteSubscribedSince()
    {
        // Arrange
        var subscription = Subscription.Create(TenantId.NewId());
        var firstActivation = DateTimeOffset.Parse("2026-01-15T10:00:00Z");
        subscription.SetStripeSubscription(new StripeSubscriptionId("sub_test"), SubscriptionPlan.Standard, 29.99m, "USD", firstActivation.AddDays(30), null, firstActivation);
        var laterUpdate = DateTimeOffset.Parse("2026-02-20T10:00:00Z");

        // Act
        subscription.SetStripeSubscription(new StripeSubscriptionId("sub_test"), SubscriptionPlan.Premium, 99.99m, "USD", laterUpdate.AddDays(30), null, laterUpdate);

        // Assert
        subscription.SubscribedSince.Should().Be(firstActivation);
    }

    [Fact]
    public void ResetToFreePlan_WhenCalled_ShouldClearSubscribedSince()
    {
        // Arrange
        var subscription = Subscription.Create(TenantId.NewId());
        var activationTime = DateTimeOffset.Parse("2026-01-15T10:00:00Z");
        subscription.SetStripeSubscription(new StripeSubscriptionId("sub_test"), SubscriptionPlan.Standard, 29.99m, "USD", activationTime.AddDays(30), null, activationTime);

        // Act
        subscription.ResetToFreePlan();

        // Assert
        subscription.SubscribedSince.Should().BeNull();
    }

    [Fact]
    public void SetStripeSubscription_WhenChangingBetweenPaidPlans_ShouldPreserveSubscribedSince()
    {
        // Arrange
        var subscription = Subscription.Create(TenantId.NewId());
        var firstActivation = DateTimeOffset.Parse("2026-01-15T10:00:00Z");
        subscription.SetStripeSubscription(new StripeSubscriptionId("sub_test"), SubscriptionPlan.Standard, 29.99m, "USD", firstActivation.AddDays(30), null, firstActivation);
        var upgradeTime = DateTimeOffset.Parse("2026-02-20T10:00:00Z");
        subscription.SetStripeSubscription(new StripeSubscriptionId("sub_test"), SubscriptionPlan.Premium, 99.99m, "USD", upgradeTime.AddDays(30), null, upgradeTime);
        var downgradeTime = DateTimeOffset.Parse("2026-03-25T10:00:00Z");

        // Act - downgrade Premium back to Standard
        subscription.SetStripeSubscription(new StripeSubscriptionId("sub_test"), SubscriptionPlan.Standard, 29.99m, "USD", downgradeTime.AddDays(30), null, downgradeTime);

        // Assert - SubscribedSince must remain the original first activation date through both paid-plan changes
        subscription.SubscribedSince.Should().Be(firstActivation);
    }

    [Fact]
    public void SetStripeSubscription_WhenReactivatingAfterReset_ShouldCaptureNewSubscribedSince()
    {
        // Arrange
        var subscription = Subscription.Create(TenantId.NewId());
        var firstActivation = DateTimeOffset.Parse("2026-01-15T10:00:00Z");
        subscription.SetStripeSubscription(new StripeSubscriptionId("sub_first"), SubscriptionPlan.Standard, 29.99m, "USD", firstActivation.AddDays(30), null, firstActivation);
        subscription.ResetToFreePlan();
        var reactivation = DateTimeOffset.Parse("2026-04-01T10:00:00Z");

        // Act
        subscription.SetStripeSubscription(new StripeSubscriptionId("sub_second"), SubscriptionPlan.Standard, 29.99m, "USD", reactivation.AddDays(30), null, reactivation);

        // Assert
        subscription.SubscribedSince.Should().Be(reactivation);
    }
}
