using Account.Features.Subscriptions.Domain;
using Account.Features.Subscriptions.Shared;
using FluentAssertions;
using SharedKernel.Domain;
using Xunit;

namespace Account.Tests.Subscriptions;

public sealed class BillingDriftDetectorTests
{
    [Fact]
    public void Detect_WhenSubscriptionMatchesStripe_ShouldReturnEmpty()
    {
        // Arrange
        var subscription = Subscription.Create(TenantId.NewId());
        subscription.SetStripeSubscription(null, SubscriptionPlan.Premium, 99.00m, "DKK", DateTimeOffset.UtcNow.AddDays(30), null, DateTimeOffset.UtcNow);
        var snapshot = new StripeSyncSnapshot(SubscriptionPlan.Premium, false, 99.00m, "DKK");

        // Act
        var discrepancies = BillingDriftDetector.Detect(subscription, snapshot, 0);

        // Assert
        discrepancies.Should().BeEmpty();
    }

    [Fact]
    public void Detect_WhenPlanDiffers_ShouldReturnCriticalDiscrepancy()
    {
        // Arrange
        var subscription = Subscription.Create(TenantId.NewId());
        subscription.SetStripeSubscription(null, SubscriptionPlan.Standard, 49.00m, "DKK", DateTimeOffset.UtcNow.AddDays(30), null, DateTimeOffset.UtcNow);
        var snapshot = new StripeSyncSnapshot(SubscriptionPlan.Premium, false, 49.00m, "DKK");

        // Act
        var discrepancies = BillingDriftDetector.Detect(subscription, snapshot, 0);

        // Assert
        discrepancies.Should().ContainSingle();
        discrepancies[0].Kind.Should().Be(DriftDiscrepancyKind.SubscriptionStateMismatch);
        discrepancies[0].Severity.Should().Be(DriftSeverity.Critical);
        discrepancies[0].ActualValue.Should().Be(nameof(SubscriptionPlan.Standard));
        discrepancies[0].ExpectedValue.Should().Be(nameof(SubscriptionPlan.Premium));
    }

    [Fact]
    public void Detect_WhenCancelAtPeriodEndDiffers_ShouldReturnWarningDiscrepancy()
    {
        // Arrange
        var subscription = Subscription.Create(TenantId.NewId());
        subscription.SetStripeSubscription(null, SubscriptionPlan.Standard, 49.00m, "DKK", DateTimeOffset.UtcNow.AddDays(30), null, DateTimeOffset.UtcNow);
        subscription.SetCancellation(false, null, null);
        var snapshot = new StripeSyncSnapshot(SubscriptionPlan.Standard, true, 49.00m, "DKK");

        // Act
        var discrepancies = BillingDriftDetector.Detect(subscription, snapshot, 0);

        // Assert
        discrepancies.Should().ContainSingle();
        discrepancies[0].Kind.Should().Be(DriftDiscrepancyKind.SubscriptionStateMismatch);
        discrepancies[0].Severity.Should().Be(DriftSeverity.Warning);
    }

    [Fact]
    public void Detect_WhenMultipleFieldsDiffer_ShouldReturnAllDiscrepancies()
    {
        // Arrange
        var subscription = Subscription.Create(TenantId.NewId());
        subscription.SetStripeSubscription(null, SubscriptionPlan.Standard, 49.00m, "DKK", DateTimeOffset.UtcNow.AddDays(30), null, DateTimeOffset.UtcNow);
        var snapshot = new StripeSyncSnapshot(SubscriptionPlan.Premium, false, 99.00m, "USD");

        // Act
        var discrepancies = BillingDriftDetector.Detect(subscription, snapshot, 0);

        // Assert
        discrepancies.Should().HaveCount(3);
        discrepancies.Select(d => d.Kind).Should().AllBeEquivalentTo(DriftDiscrepancyKind.SubscriptionStateMismatch);
    }

    [Fact]
    public void Detect_WhenPaymentTransactionsExistButNoBillingEvents_ShouldReturnMissingEventDiscrepancy()
    {
        // Arrange
        var subscription = Subscription.Create(TenantId.NewId());
        subscription.SetStripeSubscription(null, SubscriptionPlan.Premium, 99.00m, "DKK", DateTimeOffset.UtcNow.AddDays(30), null, DateTimeOffset.UtcNow);
        subscription.SetPaymentTransactions(
            [new PaymentTransaction(PaymentTransactionId.NewId(), 99.00m, 99.00m, 0m, "DKK", PaymentTransactionStatus.Succeeded, DateTimeOffset.UtcNow, null, null, null)]
        );
        var snapshot = new StripeSyncSnapshot(SubscriptionPlan.Premium, false, 99.00m, "DKK");

        // Act
        var discrepancies = BillingDriftDetector.Detect(subscription, snapshot, 0);

        // Assert
        discrepancies.Should().ContainSingle();
        discrepancies[0].Kind.Should().Be(DriftDiscrepancyKind.MissingEvent);
        discrepancies[0].Severity.Should().Be(DriftSeverity.Warning);
    }

    [Fact]
    public void Detect_WhenPaymentTransactionsAndBillingEventsBothPresent_ShouldNotReturnMissingEventDiscrepancy()
    {
        // Arrange
        var subscription = Subscription.Create(TenantId.NewId());
        subscription.SetStripeSubscription(null, SubscriptionPlan.Premium, 99.00m, "DKK", DateTimeOffset.UtcNow.AddDays(30), null, DateTimeOffset.UtcNow);
        subscription.SetPaymentTransactions(
            [new PaymentTransaction(PaymentTransactionId.NewId(), 99.00m, 99.00m, 0m, "DKK", PaymentTransactionStatus.Succeeded, DateTimeOffset.UtcNow, null, null, null)]
        );
        var snapshot = new StripeSyncSnapshot(SubscriptionPlan.Premium, false, 99.00m, "DKK");

        // Act
        var discrepancies = BillingDriftDetector.Detect(subscription, snapshot, 1);

        // Assert
        discrepancies.Should().BeEmpty();
    }

    [Fact]
    public void Detect_WhenSubscriptionHasDriftSet_AndAcknowledged_ShouldClearFlag()
    {
        // Arrange
        var subscription = Subscription.Create(TenantId.NewId());
        var discrepancy = new DriftDiscrepancy(DriftDiscrepancyKind.SubscriptionStateMismatch, "Plan mismatch.", DriftSeverity.Critical);
        subscription.SetDriftStatus([discrepancy], DateTimeOffset.UtcNow);

        // Act
        subscription.AcknowledgeDrift(DateTimeOffset.UtcNow);

        // Assert
        subscription.HasDriftDetected.Should().BeFalse();
        subscription.DriftDiscrepancies.Should().ContainSingle("acknowledgement preserves the discrepancy list for audit");
    }
}
