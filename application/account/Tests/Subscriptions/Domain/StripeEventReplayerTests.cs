using Account.Features.Subscriptions.Domain;
using Account.Features.Subscriptions.Shared;
using Account.Features.Tenants.Domain;
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
        subscription.SetStripeSubscription(new StripeSubscriptionId("sub_test"), SubscriptionPlan.Standard, 29m, "USD", now.AddDays(30), null);
        return subscription;
    }

    [Fact]
    public void Replay_WhenInvoicePaymentFailedHasFirstAttemptOnRecurringCycle_ShouldEmitPaymentFailed()
    {
        // the classification fix drops the attempt_count > 1 guard so first-attempt failures on a
        // recurring billing cycle now emit PaymentFailed (previously they were silently dropped to NoOp).
        // Arrange
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
        // the classification fix routes the latest_invoice-only branch to NoOp. The renewal signal lives
        // on the paired invoice.payment_succeeded event (covered by the next test), so emitting
        // SubscriptionRenewed here would double-count a normal recurring renewal.
        // Arrange
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

        // exactly one row (the 1:1 invariant) and it's a NoOp so it doesn't pollute the timeline.
        // Assert
        emitted.Should().HaveCount(1);
        emitted[0].EventType.Should().Be(BillingEventType.NoOp);
        emitted[0].StripeEventId.Should().Be("evt_subscription_updated_latest_invoice");
    }

    [Fact]
    public void Replay_WhenInvoicePaymentSucceededOnRecurringCycle_ShouldEmitSubscriptionRenewed()
    {
        // happy-path renewal: invoice.payment_succeeded with billing_reason=subscription_cycle
        // and a single attempt is the canonical SubscriptionRenewed signal (PaymentRecovered fires only
        // when attempt_count > 1).
        // Arrange
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
        // status: active → past_due is the dedicated SubscriptionPastDue signal introduced by PR 6.
        // Pairs with the PaymentFailed row from the corresponding invoice.payment_failed event at the same
        // timestamp; both rows describe different facets of the same business event.
        // Arrange
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
        // Stripe escalates past_due to unpaid further into the dunning cycle. From our perspective
        // both statuses are the same business state, so they share the SubscriptionPastDue event type.
        // Arrange
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

    [Fact]
    public void Replay_WhenSubscriptionDeletedAfterResetToFreePlan_ShouldStampCurrencyFromPayload()
    {
        // reproduces the C1 terminal-state currency bug. The sync flow runs
        // SyncStateFromStripe BEFORE SyncBillingEventsAsync; for terminal-state branches that flow nulls
        // Subscription.CurrentPriceCurrency via ResetToFreePlan. By the time the replayer reads the live
        // subscription, the currency is gone and the old "?? "USD"" fallback would mis-stamp DKK rows
        // with USD. The fix sources the currency from the Stripe event payload itself.
        // Arrange
        var subscription = Subscription.Create(TenantId.NewId());
        // Explicitly null — represents the post-ResetToFreePlan state at the moment Replay runs.
        subscription.ResetToFreePlan();
        subscription.CurrentPriceCurrency.Should().BeNull();

        var occurredAt = DateTimeOffset.Parse("2026-02-15T10:00:00Z");
        var stripeEvents = new[]
        {
            new StripeReplayEvent(
                "evt_subscription_deleted_dkk",
                "customer.subscription.deleted",
                occurredAt,
                """{"data":{"object":{"currency":"dkk","status":"canceled","cancel_at_period_end":false}}}""",
                MockApiVersion
            )
        };

        // Act
        var emitted = StripeEventReplayer.Replay(subscription, stripeEvents, PlanByPriceId, PriceByPlan);

        // Assert
        emitted.Should().HaveCount(1);
        emitted[0].EventType.Should().Be(BillingEventType.SubscriptionImmediatelyCancelled);
        emitted[0].Currency.Should().Be(MockStripeClient.MockStandardCurrency, "currency must come from the Stripe event payload, not the post-ResetToFreePlan subscription");
    }

    [Fact]
    public void Replay_WhenSubscriptionCreatedHasItemsPriceCurrency_ShouldStampCurrencyFromPriceItem()
    {
        // pre-subscription customer events (BillingInfoAdded, PaymentMethodUpdated) and the
        // initial customer.subscription.created event fire BEFORE the local subscription has any
        // CurrentPriceCurrency value, so the payload's items.data[0].price.currency is the only
        // authoritative source.
        // Arrange
        var subscription = Subscription.Create(TenantId.NewId());
        subscription.CurrentPriceCurrency.Should().BeNull();

        var occurredAt = DateTimeOffset.Parse("2026-01-15T10:00:00Z");
        var stripeEvents = new[]
        {
            new StripeReplayEvent(
                "evt_subscription_created_dkk",
                "customer.subscription.created",
                occurredAt,
                """{"data":{"object":{"items":{"data":[{"price":{"id":"price_standard","currency":"dkk"}}]}}}}""",
                MockApiVersion
            )
        };

        // Act
        var emitted = StripeEventReplayer.Replay(subscription, stripeEvents, PlanByPriceId, PriceByPlan);

        // Assert
        emitted.Should().HaveCount(1);
        emitted[0].EventType.Should().Be(BillingEventType.SubscriptionCreated);
        emitted[0].Currency.Should().Be(MockStripeClient.MockStandardCurrency, "currency must come from items.data[0].price.currency when the top-level currency field is absent");
    }

    [Fact]
    public void Replay_WhenPayloadHasNoCurrencyButOverrideProvided_ShouldStampCurrencyFromOverride()
    {
        // fallback path: payload doesn't carry currency (e.g. customer.created, payment_method.attached)
        // and the live subscription has been reset by an earlier branch in the same sync transaction. The
        // caller passes a snapshot of CurrentPriceCurrency captured BEFORE the mutation as currencyOverride.
        // Arrange
        var subscription = Subscription.Create(TenantId.NewId());
        subscription.ResetToFreePlan();
        subscription.CurrentPriceCurrency.Should().BeNull();

        var occurredAt = DateTimeOffset.Parse("2026-01-15T10:00:00Z");
        var stripeEvents = new[]
        {
            new StripeReplayEvent(
                "evt_payment_method_attached",
                "payment_method.attached",
                occurredAt,
                """{"data":{"object":{"id":"pm_test","type":"card"}}}""",
                MockApiVersion
            )
        };

        // Act
        var emitted = StripeEventReplayer.Replay(subscription, stripeEvents, PlanByPriceId, PriceByPlan, currencyOverride: MockStripeClient.MockStandardCurrency);

        // Assert
        emitted.Should().HaveCount(1);
        emitted[0].EventType.Should().Be(BillingEventType.PaymentMethodUpdated);
        emitted[0].Currency.Should().Be(MockStripeClient.MockStandardCurrency);
    }

    [Fact]
    public void Replay_WhenSubscriptionDeletedFollowsCancelAtPeriodEndToggle_ShouldEmitSubscriptionExpired()
    {
        // voluntary period-end cancel. The customer.subscription.updated event flips
        // cancel_at_period_end from false to true (which the replayer tracks via state.CancelAtPeriodEnd),
        // and Stripe later emits customer.subscription.deleted at period end. Stripe clears cape on the
        // deletion payload, so the period-end-vs-immediate distinction must come from prior state, not
        // the deletion payload itself.
        // Arrange
        var subscription = CreateActiveSubscription();
        var cancelToggledAt = DateTimeOffset.Parse("2026-02-15T10:00:00Z");
        var expiredAt = DateTimeOffset.Parse("2026-03-15T10:00:00Z");
        var stripeEvents = new[]
        {
            new StripeReplayEvent(
                "evt_subscription_cape_true",
                "customer.subscription.updated",
                cancelToggledAt,
                """{"data":{"object":{"items":{"data":[{"price":{"id":"price_standard","currency":"usd"}}]},"cancel_at_period_end":true,"status":"active"},"previous_attributes":{"cancel_at_period_end":false}}}""",
                MockApiVersion
            ),
            new StripeReplayEvent(
                "evt_subscription_deleted_period_end",
                "customer.subscription.deleted",
                expiredAt,
                """{"data":{"object":{"currency":"usd","status":"canceled","cancel_at_period_end":false,"cancellation_details":{"reason":"cancellation_requested"}}}}""",
                MockApiVersion
            )
        };

        // Act
        var emitted = StripeEventReplayer.Replay(subscription, stripeEvents, PlanByPriceId, PriceByPlan);

        // first row is the cancellation toggle, second row is the period-end expiry.
        // Assert
        emitted.Should().HaveCount(2);
        emitted[0].EventType.Should().Be(BillingEventType.SubscriptionCancelled);
        emitted[1].EventType.Should().Be(BillingEventType.SubscriptionExpired);
        emitted[1].StripeEventId.Should().Be("evt_subscription_deleted_period_end");
    }

    [Fact]
    public void Replay_WhenSubscriptionDeletedHasPaymentFailedReason_ShouldEmitSubscriptionSuspended()
    {
        // dunning termination. Stripe escalates a past_due/unpaid subscription to canceled and
        // sets cancellation_details.reason=payment_failed on the deletion payload. The audit ledger must
        // attribute this to involuntary churn, not voluntary cancellation.
        // Arrange
        var subscription = CreateActiveSubscription();
        var occurredAt = DateTimeOffset.Parse("2026-02-15T10:00:00Z");
        var stripeEvents = new[]
        {
            new StripeReplayEvent(
                "evt_subscription_deleted_dunning",
                "customer.subscription.deleted",
                occurredAt,
                """{"data":{"object":{"currency":"usd","status":"canceled","cancel_at_period_end":false,"cancellation_details":{"reason":"payment_failed"}}}}""",
                MockApiVersion
            )
        };

        // Act
        var emitted = StripeEventReplayer.Replay(subscription, stripeEvents, PlanByPriceId, PriceByPlan);

        // Assert
        emitted.Should().HaveCount(1);
        emitted[0].EventType.Should().Be(BillingEventType.SubscriptionSuspended);
        emitted[0].SuspensionReason.Should().Be(SuspensionReason.PaymentFailed);
        emitted[0].StripeEventId.Should().Be("evt_subscription_deleted_dunning");
    }

    [Fact]
    public void Replay_WhenSubscriptionDeletedWithoutCancelAtPeriodEndOrPaymentFailed_ShouldEmitSubscriptionImmediatelyCancelled()
    {
        // admin-initiated immediate cancel: no prior cape=true update event in the replay
        // sequence and reason=cancellation_requested rather than payment_failed. The classification falls
        // through to SubscriptionImmediatelyCancelled.
        // Arrange
        var subscription = CreateActiveSubscription();
        var occurredAt = DateTimeOffset.Parse("2026-02-15T10:00:00Z");
        var stripeEvents = new[]
        {
            new StripeReplayEvent(
                "evt_subscription_deleted_immediate",
                "customer.subscription.deleted",
                occurredAt,
                """{"data":{"object":{"currency":"usd","status":"canceled","cancel_at_period_end":false,"cancellation_details":{"reason":"cancellation_requested"}}}}""",
                MockApiVersion
            )
        };

        // Act
        var emitted = StripeEventReplayer.Replay(subscription, stripeEvents, PlanByPriceId, PriceByPlan);

        // Assert
        emitted.Should().HaveCount(1);
        emitted[0].EventType.Should().Be(BillingEventType.SubscriptionImmediatelyCancelled);
        emitted[0].SuspensionReason.Should().BeNull();
        emitted[0].StripeEventId.Should().Be("evt_subscription_deleted_immediate");
    }

    [Fact]
    public void Replay_WhenSubscriptionUpdatedPlanChangeCarriesUnitAmount_ShouldEmitNewAmountFromPayload()
    {
        // admin archives the active Premium price and replaces it with a new active price for the
        // same plan. priceByPlan[Premium] now reflects the new catalog price (159.00), but this specific
        // subscription is on the old locked-in price (149.00) carried by the payload itself. The payload
        // must win — otherwise replayed BillingEvent.NewAmount diverges from Subscription.CurrentPriceAmount
        // and the drift banner fires permanently for legacy subscriptions.
        // Arrange
        var subscription = CreateActiveSubscription();
        var priceByPlanWithRecatalogedPremium = new Dictionary<SubscriptionPlan, decimal>
        {
            [SubscriptionPlan.Standard] = 29m,
            [SubscriptionPlan.Premium] = 159m
        };
        var occurredAt = DateTimeOffset.Parse("2026-02-15T10:00:00Z");
        var stripeEvents = new[]
        {
            new StripeReplayEvent(
                "evt_subscription_upgraded_locked_in_price",
                "customer.subscription.updated",
                occurredAt,
                """{"data":{"object":{"items":{"data":[{"price":{"id":"price_premium","unit_amount":14900,"currency":"usd"}}]},"status":"active"},"previous_attributes":{"items":{"data":[{"price":{"id":"price_standard"}}]}}}}""",
                MockApiVersion
            )
        };

        // Act
        var emitted = StripeEventReplayer.Replay(subscription, stripeEvents, PlanByPriceId, priceByPlanWithRecatalogedPremium);

        // NewAmount comes from the payload's unit_amount (14900 / 100 = 149.00), NOT priceByPlan[Premium] (159.00).
        // Assert
        emitted.Should().HaveCount(1);
        emitted[0].EventType.Should().Be(BillingEventType.SubscriptionUpgraded);
        emitted[0].NewAmount.Should().Be(149.00m, "the payload's locked-in unit_amount is authoritative over the live catalog");
    }

    [Fact]
    public void Replay_WhenSubscriptionCreatedPayloadHasNoUnitAmount_ShouldFallBackToPriceByPlan()
    {
        // older subscription events (or schedule events) may omit unit_amount on the embedded
        // price object. In that case the catalog priceByPlan remains the fallback so NewAmount is still
        // populated rather than zeroed out.
        // Arrange
        var subscription = Subscription.Create(TenantId.NewId());
        var priceByPlanWithStandard = new Dictionary<SubscriptionPlan, decimal>
        {
            [SubscriptionPlan.Standard] = 29.99m,
            [SubscriptionPlan.Premium] = 99m
        };
        var occurredAt = DateTimeOffset.Parse("2026-01-15T10:00:00Z");
        var stripeEvents = new[]
        {
            new StripeReplayEvent(
                "evt_subscription_created_no_unit_amount",
                "customer.subscription.created",
                occurredAt,
                """{"data":{"object":{"items":{"data":[{"price":{"id":"price_standard","currency":"usd"}}]}}}}""",
                MockApiVersion
            )
        };

        // Act
        var emitted = StripeEventReplayer.Replay(subscription, stripeEvents, PlanByPriceId, priceByPlanWithStandard);

        // NewAmount falls back to priceByPlan[Standard] = 29.99 since the payload has no unit_amount.
        // Assert
        emitted.Should().HaveCount(1);
        emitted[0].EventType.Should().Be(BillingEventType.SubscriptionCreated);
        emitted[0].NewAmount.Should().Be(29.99m, "without unit_amount in the payload, the catalog lookup is the fallback");
    }

    [Fact]
    public void Replay_WhenRevenueEventHasNoCurrencyResolvable_ShouldSkipEventWithoutEmitting()
    {
        // every source is exhausted: payload has no currency, no override, and the subscription
        // has been reset. Refusing to emit is preferable to guessing "USD" — the row would be permanently
        // wrong on the append-only billing_events log. Only revenue-bearing events are gated this way;
        // customer-lifecycle events (customer.created/updated, payment_method.attached) carry no currency
        // in Stripe's data model and emit with null currency instead.
        // Arrange
        var subscription = Subscription.Create(TenantId.NewId());
        subscription.ResetToFreePlan();
        subscription.CurrentPriceCurrency.Should().BeNull();

        var occurredAt = DateTimeOffset.Parse("2026-01-15T10:00:00Z");
        var stripeEvents = new[]
        {
            new StripeReplayEvent(
                "evt_invoice_payment_succeeded_no_currency",
                "invoice.payment_succeeded",
                occurredAt,
                """{"data":{"object":{"id":"in_test","amount_paid":2999,"status":"paid"}}}""",
                MockApiVersion
            )
        };

        // Act
        var emitted = StripeEventReplayer.Replay(subscription, stripeEvents, PlanByPriceId, PriceByPlan);

        // Assert
        emitted.Should().BeEmpty("no currency could be resolved from any source — the replayer must refuse to emit rather than guess");
    }

    [Fact]
    public void Replay_WhenCustomerLifecycleEventHasNoCurrencyResolvable_ShouldEmitWithNullCurrency()
    {
        // payment_method.attached and customer.created/updated have no currency in Stripe's data model —
        // currency belongs to prices and invoices. The replayer must emit these even when no currency
        // is resolvable from any source, with null currency on the resulting BillingEvent row.
        // Arrange
        var subscription = Subscription.Create(TenantId.NewId());
        subscription.ResetToFreePlan();
        subscription.CurrentPriceCurrency.Should().BeNull();

        var occurredAt = DateTimeOffset.Parse("2026-01-15T10:00:00Z");
        var stripeEvents = new[]
        {
            new StripeReplayEvent(
                "evt_payment_method_attached_no_currency",
                "payment_method.attached",
                occurredAt,
                """{"data":{"object":{"id":"pm_test","type":"card"}}}""",
                MockApiVersion
            )
        };

        // Act
        var emitted = StripeEventReplayer.Replay(subscription, stripeEvents, PlanByPriceId, PriceByPlan);

        // Assert
        emitted.Should().HaveCount(1);
        emitted[0].EventType.Should().Be(BillingEventType.PaymentMethodUpdated);
        emitted[0].Currency.Should().BeNull();
    }
}
