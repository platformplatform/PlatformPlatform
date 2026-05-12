using Account.Features.Subscriptions.Domain;
using Account.Features.Subscriptions.Shared;
using Account.Integrations.Stripe;
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
        var localSnapshot = new StripeSyncSnapshot(SubscriptionPlan.Premium, false, 99.00m, MockStripeClient.MockStandardCurrency);
        var stripeSnapshot = new StripeSyncSnapshot(SubscriptionPlan.Premium, false, 99.00m, MockStripeClient.MockStandardCurrency);

        // Act
        var discrepancies = BillingDriftDetector.Detect(localSnapshot, stripeSnapshot, 0, 0);

        // Assert
        discrepancies.Should().BeEmpty();
    }

    [Fact]
    public void Detect_WhenPlanDiffers_ShouldReturnCriticalDiscrepancy()
    {
        // Arrange
        var localSnapshot = new StripeSyncSnapshot(SubscriptionPlan.Standard, false, 49.00m, MockStripeClient.MockStandardCurrency);
        var stripeSnapshot = new StripeSyncSnapshot(SubscriptionPlan.Premium, false, 49.00m, MockStripeClient.MockStandardCurrency);

        // Act
        var discrepancies = BillingDriftDetector.Detect(localSnapshot, stripeSnapshot, 0, 0);

        // Assert
        discrepancies.Should().ContainSingle();
        discrepancies[0].Kind.Should().Be(DriftDiscrepancyKind.SubscriptionStateMismatch);
        discrepancies[0].Severity.Should().Be(DriftSeverity.Critical);
        discrepancies[0].ActualValue.Should().Be(nameof(SubscriptionPlan.Standard));
        discrepancies[0].ExpectedValue.Should().Be(nameof(SubscriptionPlan.Premium));
    }

    [Fact]
    public void Detect_WhenStripeSaysPremiumButLocalSaysStandard_ShouldFireSubscriptionStateMismatch()
    {
        // Regression: snapshot used to be built from the just-mutated subscription, making mismatch unreachable.
        // Arrange
        var localSnapshot = new StripeSyncSnapshot(SubscriptionPlan.Standard, false, 49.00m, MockStripeClient.MockStandardCurrency);
        var stripeSnapshot = new StripeSyncSnapshot(SubscriptionPlan.Premium, false, 99.00m, MockStripeClient.MockStandardCurrency);

        // Act
        var discrepancies = BillingDriftDetector.Detect(localSnapshot, stripeSnapshot, 0, 0);

        // Assert
        discrepancies.Should().HaveCount(2);
        discrepancies.Select(d => d.Kind).Should().AllBeEquivalentTo(DriftDiscrepancyKind.SubscriptionStateMismatch);
        discrepancies.Should().Contain(d =>
            d.ExpectedValue == nameof(SubscriptionPlan.Premium) && d.ActualValue == nameof(SubscriptionPlan.Standard) && d.Description == "Plan differs between local subscription and Stripe."
        );
        discrepancies.Should().Contain(d => d.Description == "Current price amount differs between local subscription and Stripe.");
    }

    [Fact]
    public void Detect_WhenCancelAtPeriodEndDiffers_ShouldReturnWarningDiscrepancy()
    {
        // Arrange
        var localSnapshot = new StripeSyncSnapshot(SubscriptionPlan.Standard, false, 49.00m, MockStripeClient.MockStandardCurrency);
        var stripeSnapshot = new StripeSyncSnapshot(SubscriptionPlan.Standard, true, 49.00m, MockStripeClient.MockStandardCurrency);

        // Act
        var discrepancies = BillingDriftDetector.Detect(localSnapshot, stripeSnapshot, 0, 0);

        // Assert
        discrepancies.Should().ContainSingle();
        discrepancies[0].Kind.Should().Be(DriftDiscrepancyKind.SubscriptionStateMismatch);
        discrepancies[0].Severity.Should().Be(DriftSeverity.Warning);
    }

    [Fact]
    public void Detect_WhenMultipleFieldsDiffer_ShouldReturnAllDiscrepancies()
    {
        // Arrange
        var localSnapshot = new StripeSyncSnapshot(SubscriptionPlan.Standard, false, 49.00m, MockStripeClient.MockStandardCurrency);
        var stripeSnapshot = new StripeSyncSnapshot(SubscriptionPlan.Premium, false, 99.00m, "USD");

        // Act
        var discrepancies = BillingDriftDetector.Detect(localSnapshot, stripeSnapshot, 0, 0);

        // Assert
        discrepancies.Should().HaveCount(3);
        discrepancies.Select(d => d.Kind).Should().AllBeEquivalentTo(DriftDiscrepancyKind.SubscriptionStateMismatch);
    }

    [Fact]
    public void Detect_WhenPaymentTransactionsExistButNoBillingEvents_ShouldReturnMissingEventDiscrepancy()
    {
        // Arrange
        var localSnapshot = new StripeSyncSnapshot(SubscriptionPlan.Premium, false, 99.00m, MockStripeClient.MockStandardCurrency);
        var stripeSnapshot = new StripeSyncSnapshot(SubscriptionPlan.Premium, false, 99.00m, MockStripeClient.MockStandardCurrency);

        // Act
        var discrepancies = BillingDriftDetector.Detect(localSnapshot, stripeSnapshot, 1, 0);

        // Assert
        discrepancies.Should().ContainSingle();
        discrepancies[0].Kind.Should().Be(DriftDiscrepancyKind.MissingEvent);
        discrepancies[0].Severity.Should().Be(DriftSeverity.Warning);
    }

    [Fact]
    public void Detect_WhenPaymentTransactionsAndBillingEventsBothPresent_ShouldNotReturnMissingEventDiscrepancy()
    {
        // Arrange
        var localSnapshot = new StripeSyncSnapshot(SubscriptionPlan.Premium, false, 99.00m, MockStripeClient.MockStandardCurrency);
        var stripeSnapshot = new StripeSyncSnapshot(SubscriptionPlan.Premium, false, 99.00m, MockStripeClient.MockStandardCurrency);

        // Act
        var discrepancies = BillingDriftDetector.Detect(localSnapshot, stripeSnapshot, 1, 1);

        // Assert
        discrepancies.Should().BeEmpty();
    }

    [Fact]
    public void StripeSyncSnapshot_FromSubscription_ShouldCaptureCurrentLocalState()
    {
        // Arrange
        var subscription = Subscription.Create(TenantId.NewId());
        subscription.SetStripeSubscription(null, SubscriptionPlan.Premium, 99.00m, MockStripeClient.MockStandardCurrency, DateTimeOffset.UtcNow.AddDays(30), null);
        subscription.SetCancellation(true, null, null);

        // Act
        var snapshot = StripeSyncSnapshot.FromSubscription(subscription);

        // Assert
        snapshot.Plan.Should().Be(SubscriptionPlan.Premium);
        snapshot.CancelAtPeriodEnd.Should().BeTrue();
        snapshot.CurrentPriceAmount.Should().Be(99.00m);
        snapshot.CurrentPriceCurrency.Should().Be(MockStripeClient.MockStandardCurrency);
    }

    [Fact]
    public void Detect_WhenLocalHasScheduledPlanButScheduledPriceIsNull_ShouldReturnScheduledPriceMissingDiscrepancy()
    {
        // Defends MrrCalculator.ForwardMrr's ScheduledPriceAmount ?? CurrentPriceAmount rule. A subscription
        // with ScheduledPlan set but ScheduledPriceAmount null silently distorts BLENDED MRR (falls back to the
        // higher current price). The cancel-then-reschedule pair in a single sync window left scheduled_price
        // _amount NULL, motivating this check.
        // Arrange
        var localSnapshot = new StripeSyncSnapshot(SubscriptionPlan.Premium, false, 99.00m, MockStripeClient.MockStandardCurrency, SubscriptionPlan.Standard);
        var stripeSnapshot = new StripeSyncSnapshot(SubscriptionPlan.Premium, false, 99.00m, MockStripeClient.MockStandardCurrency);

        // Act
        var discrepancies = BillingDriftDetector.Detect(localSnapshot, stripeSnapshot, 0, 0);

        // Assert
        discrepancies.Should().ContainSingle();
        discrepancies[0].Kind.Should().Be(DriftDiscrepancyKind.ScheduledPriceMissing);
        discrepancies[0].Severity.Should().Be(DriftSeverity.Critical);
        discrepancies[0].ExpectedValue.Should().Be(nameof(SubscriptionPlan.Standard));
    }

    [Fact]
    public void Detect_WhenLocalHasScheduledPlanAndScheduledPrice_ShouldNotReturnScheduledPriceMissingDiscrepancy()
    {
        // Happy path: scheduled plan and price are both set.
        // Arrange
        var localSnapshot = new StripeSyncSnapshot(SubscriptionPlan.Premium, false, 99.00m, MockStripeClient.MockStandardCurrency, SubscriptionPlan.Standard, 29.00m);
        var stripeSnapshot = new StripeSyncSnapshot(SubscriptionPlan.Premium, false, 99.00m, MockStripeClient.MockStandardCurrency);

        // Act
        var discrepancies = BillingDriftDetector.Detect(localSnapshot, stripeSnapshot, 0, 0);

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
