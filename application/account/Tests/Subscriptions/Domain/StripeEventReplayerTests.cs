using Account.Features.Subscriptions.Domain;
using Account.Features.Subscriptions.Shared;
using Account.Integrations.Stripe;
using FluentAssertions;
using SharedKernel.Domain;
using Xunit;

namespace Account.Tests.Subscriptions.Domain;

/// <summary>
///     Unit tests for <see cref="StripeEventReplayer.Replay" /> covering the classification rules added
///     in the multi-source reconciliation work. Scope is the rules that are easiest to verify with
///     hand-crafted JSON fixtures: PaymentFailed for first-attempt subscription_cycle failures,
///     SubscriptionRenewed via invoice.payment_succeeded, SubscriptionPastDue on status active→past_due
///     and active→unpaid, and the latest_invoice→NoOp invariant. Cancellation, reactivation, upgrade,
///     downgrade, and schedule branches are covered end-to-end through webhook fixtures in
///     <see cref="BillingEventAppendTests" />, so this file deliberately doesn't duplicate them.
/// </summary>
public sealed class StripeEventReplayerTests
{
    private const string MockApiVersion = "2025-09-30.preview";

    private static readonly IReadOnlyDictionary<string, SubscriptionPlan> PlanByPriceId = new Dictionary<string, SubscriptionPlan>
    {
        ["price_standard"] = SubscriptionPlan.Standard,
        ["price_premium"] = SubscriptionPlan.Premium
    };

    private static readonly IReadOnlyDictionary<SubscriptionPlan, decimal> PriceByPlan = new Dictionary<SubscriptionPlan, decimal>
    {
        [SubscriptionPlan.Standard] = 29m,
        [SubscriptionPlan.Premium] = 99m
    };

    private static Subscription CreateActiveSubscription()
    {
        var subscription = Subscription.Create(TenantId.NewId());
        var now = DateTimeOffset.Parse("2026-01-15T10:00:00Z");
        subscription.SetStripeSubscription(new StripeSubscriptionId("sub_test"), SubscriptionPlan.Standard, 29m, "USD", now.AddDays(30), null, now);
        return subscription;
    }

    [Fact]
    public void Replay_WhenInvoicePaymentFailedHasFirstAttemptOnRecurringCycle_ShouldEmitPaymentFailed()
    {
        // Arrange — the classification fix drops the attempt_count > 1 guard so first-attempt failures on a
        // recurring billing cycle now emit PaymentFailed (previously they were silently dropped to NoOp).
        var subscription = CreateActiveSubscription();
        var occurredAt = DateTimeOffset.Parse("2026-02-15T10:00:00Z");
        var stripeEvents = new[]
        {
            new StripeReplayEvent(
                "evt_payment_failed_attempt_1",
                "invoice.payment_failed",
                occurredAt,
                """{"data":{"object":{"attempt_count":1,"billing_reason":"subscription_cycle"}}}""",
                MockApiVersion
            )
        };

        // Act
        var emitted = StripeEventReplayer.Replay(subscription, stripeEvents, PlanByPriceId, PriceByPlan);

        // Assert
        emitted.Should().HaveCount(1);
        emitted[0].EventType.Should().Be(BillingEventType.PaymentFailed);
        emitted[0].StripeEventId.Should().Be("evt_payment_failed_attempt_1");
    }

    [Fact]
    public void Replay_WhenSubscriptionUpdatedCarriesOnlyLatestInvoiceChange_ShouldEmitNoOp()
    {
        // Arrange — the classification fix routes the latest_invoice-only branch to NoOp. The renewal signal lives
        // on the paired invoice.payment_succeeded event (covered by the next test), so emitting
        // SubscriptionRenewed here would double-count a normal recurring renewal.
        var subscription = CreateActiveSubscription();
        var occurredAt = DateTimeOffset.Parse("2026-02-15T10:00:00Z");
        var stripeEvents = new[]
        {
            new StripeReplayEvent(
                "evt_subscription_updated_latest_invoice",
                "customer.subscription.updated",
                occurredAt,
                """{"data":{"object":{"items":{"data":[{"price":{"id":"price_standard"}}]},"status":"active"},"previous_attributes":{"latest_invoice":"in_old"}}}""",
                MockApiVersion
            )
        };

        // Act
        var emitted = StripeEventReplayer.Replay(subscription, stripeEvents, PlanByPriceId, PriceByPlan);

        // Assert — exactly one row (the 1:1 invariant) and it's a NoOp so it doesn't pollute the timeline.
        emitted.Should().HaveCount(1);
        emitted[0].EventType.Should().Be(BillingEventType.NoOp);
        emitted[0].StripeEventId.Should().Be("evt_subscription_updated_latest_invoice");
    }

    [Fact]
    public void Replay_WhenInvoicePaymentSucceededOnRecurringCycle_ShouldEmitSubscriptionRenewed()
    {
        // Arrange — happy-path renewal: invoice.payment_succeeded with billing_reason=subscription_cycle
        // and a single attempt is the canonical SubscriptionRenewed signal (PaymentRecovered fires only
        // when attempt_count > 1).
        var subscription = CreateActiveSubscription();
        var occurredAt = DateTimeOffset.Parse("2026-02-15T10:00:00Z");
        var stripeEvents = new[]
        {
            new StripeReplayEvent(
                "evt_invoice_payment_succeeded",
                "invoice.payment_succeeded",
                occurredAt,
                """{"data":{"object":{"attempt_count":1,"billing_reason":"subscription_cycle"}}}""",
                MockApiVersion
            )
        };

        // Act
        var emitted = StripeEventReplayer.Replay(subscription, stripeEvents, PlanByPriceId, PriceByPlan);

        // Assert
        emitted.Should().HaveCount(1);
        emitted[0].EventType.Should().Be(BillingEventType.SubscriptionRenewed);
    }

    [Fact]
    public void Replay_WhenSubscriptionUpdatedTransitionsActiveToPastDue_ShouldEmitSubscriptionPastDue()
    {
        // Arrange — status: active → past_due is the dedicated SubscriptionPastDue signal introduced by PR 6.
        // Pairs with the PaymentFailed row from the corresponding invoice.payment_failed event at the same
        // timestamp; both rows describe different facets of the same business event.
        var subscription = CreateActiveSubscription();
        var occurredAt = DateTimeOffset.Parse("2026-02-15T10:00:00Z");
        var stripeEvents = new[]
        {
            new StripeReplayEvent(
                "evt_subscription_status_past_due",
                "customer.subscription.updated",
                occurredAt,
                """{"data":{"object":{"items":{"data":[{"price":{"id":"price_standard"}}]},"status":"past_due"},"previous_attributes":{"status":"active"}}}""",
                MockApiVersion
            )
        };

        // Act
        var emitted = StripeEventReplayer.Replay(subscription, stripeEvents, PlanByPriceId, PriceByPlan);

        // Assert
        emitted.Should().HaveCount(1);
        emitted[0].EventType.Should().Be(BillingEventType.SubscriptionPastDue);
        emitted[0].StripeEventId.Should().Be("evt_subscription_status_past_due");
    }

    [Fact]
    public void Replay_WhenSubscriptionUpdatedTransitionsActiveToUnpaid_ShouldEmitSubscriptionPastDue()
    {
        // Arrange — Stripe escalates past_due to unpaid further into the dunning cycle. From our perspective
        // both statuses are the same business state, so they share the SubscriptionPastDue event type.
        var subscription = CreateActiveSubscription();
        var occurredAt = DateTimeOffset.Parse("2026-02-15T10:00:00Z");
        var stripeEvents = new[]
        {
            new StripeReplayEvent(
                "evt_subscription_status_unpaid",
                "customer.subscription.updated",
                occurredAt,
                """{"data":{"object":{"items":{"data":[{"price":{"id":"price_standard"}}]},"status":"unpaid"},"previous_attributes":{"status":"active"}}}""",
                MockApiVersion
            )
        };

        // Act
        var emitted = StripeEventReplayer.Replay(subscription, stripeEvents, PlanByPriceId, PriceByPlan);

        // Assert
        emitted.Should().HaveCount(1);
        emitted[0].EventType.Should().Be(BillingEventType.SubscriptionPastDue);
    }
}
