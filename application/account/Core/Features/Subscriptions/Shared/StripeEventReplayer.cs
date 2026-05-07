using System.Text.Json;
using Account.Features.Subscriptions.Domain;

namespace Account.Features.Subscriptions.Shared;

/// <summary>
///     Replays a customer's stored stripe_events into the BillingEvent log. Used as a one-time backfill
///     for subscriptions persisted before the BillingEvent log existed (the live webhook path writes
///     events directly via <see cref="ProcessPendingStripeEvents" />).
///     The replayer is a state machine: it iterates events chronologically and tracks the running
///     subscription state (current plan, current price, scheduled-downgrade plan/price, cancel-at-
///     period-end flag, committed forward MRR). Each event reads its plan label and amounts from the
///     state at that point in time — never from <c>subscription.CurrentPriceAmount</c> or
///     <c>subscription.Plan</c> — so historical rows reflect the truth at the time they happened. After
///     processing each event the state advances. The "MRR after" of one row equals the "MRR before"
///     of the next, making the BillingEvent log a faithful audit trail.
///     Deterministic IDs (derived from the stripe_event Id and BillingEventType) keep the operation
///     idempotent across repeated syncs.
/// </summary>
public static class StripeEventReplayer
{
    public static IReadOnlyList<BillingEvent> Replay(
        Subscription subscription,
        StripeEvent[] stripeEvents,
        IReadOnlyDictionary<string, SubscriptionPlan> planByPriceId,
        IReadOnlyDictionary<SubscriptionPlan, decimal> priceByPlan
    )
    {
        var emitted = new List<BillingEvent>();
        var state = new ReplayState();
        var currency = subscription.CurrentPriceCurrency ?? "USD";

        foreach (var stripeEvent in stripeEvents)
        {
            var occurredAt = stripeEvent.CreatedAt;
            var stripeReference = stripeEvent.Id.Value;
            var billingEvents = MapEvent(stripeEvent, occurredAt, stripeReference, subscription, state, planByPriceId, priceByPlan, currency);
            emitted.AddRange(billingEvents);
        }

        return emitted;
    }

    private static IEnumerable<BillingEvent> MapEvent(
        StripeEvent stripeEvent,
        DateTimeOffset occurredAt,
        string stripeReference,
        Subscription subscription,
        ReplayState state,
        IReadOnlyDictionary<string, SubscriptionPlan> planByPriceId,
        IReadOnlyDictionary<SubscriptionPlan, decimal> priceByPlan,
        string currency
    )
    {
        var subscriptionId = subscription.Id;
        var tenantId = subscription.TenantId;
        var payload = ParsePayload(stripeEvent.Payload);

        switch (stripeEvent.EventType)
        {
            case "customer.created":
                yield return BillingEvent.Create(
                    subscriptionId, tenantId, BillingEventType.BillingInfoAdded, occurredAt, stripeReference
                );
                break;

            case "customer.updated":
                if (HasBillingFieldsChanged(payload))
                {
                    yield return BillingEvent.Create(
                        subscriptionId, tenantId, BillingEventType.BillingInfoUpdated, occurredAt, stripeReference
                    );
                }

                break;

            case "payment_method.attached":
                yield return BillingEvent.Create(
                    subscriptionId, tenantId, BillingEventType.PaymentMethodUpdated, occurredAt, stripeReference
                );
                break;

            case "customer.subscription.created":
            {
                var newPlan = ResolvePlanFromSubscriptionPayload(payload, planByPriceId) ?? subscription.Plan;
                var newPrice = priceByPlan.TryGetValue(newPlan, out var p) ? p : 0m;
                var previousMrr = state.CommittedMrr;
                state.Plan = newPlan;
                state.PlanPrice = newPrice;
                state.CancelAtPeriodEnd = false;
                state.ScheduledPlan = null;
                state.CommittedMrr = newPrice;
                yield return BillingEvent.Create(
                    subscriptionId, tenantId, BillingEventType.SubscriptionCreated, occurredAt, stripeReference,
                    toPlan: newPlan,
                    previousAmount: previousMrr, newAmount: newPrice,
                    amountDelta: newPrice - previousMrr,
                    currency: currency
                );
                break;
            }

            case "customer.subscription.updated":
                foreach (var billingEvent in MapSubscriptionUpdated(payload, occurredAt, stripeReference, subscription, state, planByPriceId, priceByPlan, currency))
                {
                    yield return billingEvent;
                }

                break;

            // customer.subscription.pending_update_applied fires alongside customer.subscription.updated
            // for the same upgrade transition. The updated event carries previous_attributes (the from-plan)
            // so it is the higher-fidelity source — we skip pending_update_applied to avoid emitting two
            // SubscriptionUpgraded rows for the same logical event.

            case "customer.subscription.deleted":
            {
                var eventType = MapSubscriptionDeleted(payload);
                var previousMrr = state.CommittedMrr;
                var fromPlan = state.Plan;
                state.Plan = null;
                state.PlanPrice = 0m;
                state.CancelAtPeriodEnd = false;
                state.ScheduledPlan = null;
                state.CommittedMrr = 0m;
                yield return BillingEvent.Create(
                    subscriptionId, tenantId, eventType, occurredAt, stripeReference,
                    fromPlan, SubscriptionPlan.Basis,
                    previousMrr, 0m,
                    -previousMrr,
                    currency
                );
                break;
            }

            // subscription_schedule.created carries only the current phase — the future-phase plan that
            // defines the downgrade target only shows up in the subscription_schedule.updated event that
            // fires immediately after. We skip created and let updated drive the DowngradeScheduled row.

            case "subscription_schedule.updated":
            {
                var scheduledPlan = ResolveScheduledTargetPlan(payload, planByPriceId, state.Plan);
                if (scheduledPlan is null) break;
                if (scheduledPlan == state.ScheduledPlan) break;

                var scheduledPrice = priceByPlan.TryGetValue(scheduledPlan.Value, out var sp) ? sp : 0m;
                var previousMrr = state.CommittedMrr;
                state.ScheduledPlan = scheduledPlan;
                state.CommittedMrr = scheduledPrice;
                yield return BillingEvent.Create(
                    subscriptionId, tenantId, BillingEventType.SubscriptionDowngradeScheduled, occurredAt, stripeReference,
                    state.Plan, scheduledPlan,
                    previousMrr, scheduledPrice,
                    scheduledPrice - previousMrr,
                    currency
                );
                break;
            }

            case "subscription_schedule.released":
            case "subscription_schedule.canceled":
            {
                if (state.ScheduledPlan is null) break;

                var previousMrr = state.CommittedMrr;
                var newMrr = state.PlanPrice;
                state.ScheduledPlan = null;
                state.CommittedMrr = newMrr;
                var delta = newMrr - previousMrr;
                yield return BillingEvent.Create(
                    subscriptionId, tenantId, BillingEventType.SubscriptionDowngradeCancelled, occurredAt, stripeReference,
                    toPlan: state.Plan,
                    previousAmount: previousMrr, newAmount: newMrr,
                    amountDelta: delta == 0m ? null : delta,
                    currency: currency
                );
                break;
            }

            case "invoice.payment_succeeded":
            {
                // Only emit a Renewed row for genuine recurring renewals (billing_reason == subscription_cycle).
                // subscription_create is covered by customer.subscription.created; subscription_update is the
                // proration invoice from a plan change and is covered by the customer.subscription.updated
                // upgrade/downgrade row — emitting Renewed here would duplicate it. Renewals don't change
                // committed MRR so amountDelta stays null.
                var billingReason = ExtractInvoiceBillingReason(payload);
                if (billingReason != "subscription_cycle") break;

                var eventType = HasMultiplePaymentAttempts(payload) ? BillingEventType.PaymentRecovered : BillingEventType.SubscriptionRenewed;
                yield return BillingEvent.Create(
                    subscriptionId, tenantId, eventType, occurredAt, stripeReference,
                    toPlan: state.Plan,
                    newAmount: state.CommittedMrr,
                    currency: currency
                );
                break;
            }

            case "invoice.payment_failed":
                // Skip 3DS challenges that succeed on first attempt (attempt_count == 1) — those produce a
                // payment_failed event followed shortly by payment_succeeded. Only emit PaymentFailed when
                // Stripe has retried (attempt_count > 1), which is a real persistent failure. Failures don't
                // change committed MRR — the customer is still on the plan, just behind on payment.
                if (HasMultiplePaymentAttempts(payload))
                {
                    yield return BillingEvent.Create(
                        subscriptionId, tenantId, BillingEventType.PaymentFailed, occurredAt, stripeReference,
                        toPlan: state.Plan,
                        newAmount: state.CommittedMrr,
                        currency: currency
                    );
                }

                break;

            case "charge.refunded":
            {
                // A refund is a one-time cash event, not an MRR change going forward, so amountDelta is null.
                var refundTransaction = FindClosestRefundedTransaction(subscription, occurredAt);
                yield return BillingEvent.Create(
                    subscriptionId, tenantId, BillingEventType.PaymentRefunded, occurredAt, stripeReference,
                    toPlan: state.Plan,
                    newAmount: state.CommittedMrr,
                    currency: refundTransaction?.Currency ?? currency
                );
                break;
            }
        }
    }

    private static IEnumerable<BillingEvent> MapSubscriptionUpdated(
        JsonElement payload,
        DateTimeOffset occurredAt,
        string stripeReference,
        Subscription subscription,
        ReplayState state,
        IReadOnlyDictionary<string, SubscriptionPlan> planByPriceId,
        IReadOnlyDictionary<SubscriptionPlan, decimal> priceByPlan,
        string currency
    )
    {
        var previous = payload.TryGetProperty("data", out var data) && data.TryGetProperty("previous_attributes", out var prev) ? prev : default;
        if (previous.ValueKind != JsonValueKind.Object) yield break;

        // Cancel-at-period-end toggle. Forward MRR drops at the moment the customer commits to leaving,
        // not at the effective period end — committed MRR is the leading indicator we want here.
        if (previous.TryGetProperty("cancel_at_period_end", out var prevCancel) && prevCancel.ValueKind == JsonValueKind.False)
        {
            var previousMrr = state.CommittedMrr;
            state.CancelAtPeriodEnd = true;
            state.CommittedMrr = 0m;
            yield return BillingEvent.Create(
                subscription.Id, subscription.TenantId, BillingEventType.SubscriptionCancelled, occurredAt, stripeReference,
                toPlan: state.Plan,
                previousAmount: previousMrr, newAmount: 0m, amountDelta: -previousMrr,
                currency: currency,
                cancellationReason: subscription.CancellationReason
            );
        }
        else if (previous.TryGetProperty("cancel_at_period_end", out var prevCancelTrue) && prevCancelTrue.ValueKind == JsonValueKind.True)
        {
            var previousMrr = state.CommittedMrr;
            state.CancelAtPeriodEnd = false;
            state.CommittedMrr = state.PlanPrice;
            yield return BillingEvent.Create(
                subscription.Id, subscription.TenantId, BillingEventType.SubscriptionReactivated, occurredAt, stripeReference,
                toPlan: state.Plan,
                previousAmount: previousMrr, newAmount: state.CommittedMrr,
                amountDelta: state.CommittedMrr - previousMrr,
                currency: currency
            );
        }

        // Plan change (items.data[0].price changed). MRR impact is the real price diff between plans
        // looked up from the catalog — so an upgrade Standard→Premium shows +150 and a downgrade
        // Premium→Standard shows -150.
        var newPlan = ResolvePlanFromSubscriptionPayload(payload, planByPriceId);
        var previousPlan = ResolvePlanFromPreviousAttributes(previous, planByPriceId);
        if (newPlan is not null && previousPlan is not null && newPlan != previousPlan)
        {
            var eventType = newPlan.Value > previousPlan.Value ? BillingEventType.SubscriptionUpgraded : BillingEventType.SubscriptionDowngraded;
            var previousMrr = state.CommittedMrr;
            var newPrice = priceByPlan.TryGetValue(newPlan.Value, out var np) ? np : 0m;
            state.Plan = newPlan;
            state.PlanPrice = newPrice;
            state.CommittedMrr = state.CancelAtPeriodEnd ? 0m : newPrice;
            yield return BillingEvent.Create(
                subscription.Id, subscription.TenantId, eventType, occurredAt, stripeReference,
                previousPlan, newPlan,
                previousMrr, state.CommittedMrr,
                state.CommittedMrr - previousMrr,
                currency
            );
        }
    }

    private static BillingEventType MapSubscriptionDeleted(JsonElement payload)
    {
        var data = payload.TryGetProperty("data", out var d) ? d : default;
        var sub = data.TryGetProperty("object", out var obj) ? obj : default;
        var status = sub.TryGetProperty("status", out var s) ? s.GetString() : null;
        var cancelAtPeriodEnd = sub.TryGetProperty("cancel_at_period_end", out var cape) && cape.ValueKind == JsonValueKind.True;

        if (status is "past_due" or "unpaid" or "incomplete_expired") return BillingEventType.SubscriptionSuspended;
        if (cancelAtPeriodEnd) return BillingEventType.SubscriptionExpired;
        return BillingEventType.SubscriptionImmediatelyCancelled;
    }

    private static SubscriptionPlan? ResolvePlanFromSubscriptionPayload(JsonElement payload, IReadOnlyDictionary<string, SubscriptionPlan> planByPriceId)
    {
        var data = payload.TryGetProperty("data", out var d) ? d : default;
        var sub = data.TryGetProperty("object", out var obj) ? obj : default;
        var items = sub.TryGetProperty("items", out var i) ? i : default;
        var itemsData = items.TryGetProperty("data", out var id) ? id : default;
        if (itemsData.ValueKind != JsonValueKind.Array) return null;
        foreach (var item in itemsData.EnumerateArray())
        {
            var priceId = item.TryGetProperty("price", out var price) && price.TryGetProperty("id", out var pid) ? pid.GetString() : null;
            if (priceId is not null && planByPriceId.TryGetValue(priceId, out var plan)) return plan;
        }

        return null;
    }

    private static SubscriptionPlan? ResolvePlanFromPreviousAttributes(JsonElement previousAttributes, IReadOnlyDictionary<string, SubscriptionPlan> planByPriceId)
    {
        if (!previousAttributes.TryGetProperty("items", out var items)) return null;
        if (!items.TryGetProperty("data", out var itemsData) || itemsData.ValueKind != JsonValueKind.Array) return null;
        foreach (var item in itemsData.EnumerateArray())
        {
            var priceId = item.TryGetProperty("price", out var price) && price.TryGetProperty("id", out var pid) ? pid.GetString() : null;
            if (priceId is not null && planByPriceId.TryGetValue(priceId, out var plan)) return plan;
        }

        return null;
    }

    /// <summary>
    ///     Resolves the scheduled target plan from a subscription_schedule.updated payload. The phases
    ///     array describes consecutive billing windows; the LAST phase carries the future plan after the
    ///     current period ends. Returns null when the schedule has fewer than two phases (no future
    ///     target) or when the last phase's plan equals the current plan (no actual change).
    /// </summary>
    private static SubscriptionPlan? ResolveScheduledTargetPlan(JsonElement payload, IReadOnlyDictionary<string, SubscriptionPlan> planByPriceId, SubscriptionPlan? currentPlan)
    {
        var data = payload.TryGetProperty("data", out var d) ? d : default;
        var schedule = data.TryGetProperty("object", out var obj) ? obj : default;
        var phases = schedule.TryGetProperty("phases", out var ph) ? ph : default;
        if (phases.ValueKind != JsonValueKind.Array) return null;

        SubscriptionPlan? lastPhasePlan = null;
        var phaseCount = 0;
        foreach (var phase in phases.EnumerateArray())
        {
            phaseCount++;
            var items = phase.TryGetProperty("items", out var i) ? i : default;
            if (items.ValueKind != JsonValueKind.Array) continue;
            foreach (var item in items.EnumerateArray())
            {
                var priceId = item.TryGetProperty("price", out var price) ? price.GetString() : null;
                if (priceId is not null && planByPriceId.TryGetValue(priceId, out var plan)) lastPhasePlan = plan;
            }
        }

        if (phaseCount < 2) return null;
        if (lastPhasePlan == currentPlan) return null;
        return lastPhasePlan;
    }

    private static bool HasBillingFieldsChanged(JsonElement payload)
    {
        var previous = payload.TryGetProperty("data", out var data) && data.TryGetProperty("previous_attributes", out var prev) ? prev : default;
        if (previous.ValueKind != JsonValueKind.Object) return false;
        return previous.TryGetProperty("address", out _)
               || previous.TryGetProperty("email", out _)
               || previous.TryGetProperty("name", out _)
               || previous.TryGetProperty("tax_ids", out _);
    }

    private static string? ExtractInvoiceBillingReason(JsonElement payload)
    {
        var data = payload.TryGetProperty("data", out var d) ? d : default;
        var invoice = data.TryGetProperty("object", out var obj) ? obj : default;
        return invoice.TryGetProperty("billing_reason", out var br) ? br.GetString() : null;
    }

    private static bool HasMultiplePaymentAttempts(JsonElement payload)
    {
        var data = payload.TryGetProperty("data", out var d) ? d : default;
        var invoice = data.TryGetProperty("object", out var obj) ? obj : default;
        var attemptCount = invoice.TryGetProperty("attempt_count", out var ac) && ac.ValueKind == JsonValueKind.Number ? ac.GetInt32() : 0;
        return attemptCount > 1;
    }

    private static PaymentTransaction? FindClosestRefundedTransaction(Subscription subscription, DateTimeOffset occurredAt)
    {
        return subscription.PaymentTransactions
            .Where(t => t.Status == PaymentTransactionStatus.Refunded)
            .OrderBy(t => Math.Abs((t.Date - occurredAt).TotalSeconds))
            .FirstOrDefault();
    }

    private static JsonElement ParsePayload(string? rawPayload)
    {
        if (string.IsNullOrWhiteSpace(rawPayload)) return default;
        try
        {
            using var doc = JsonDocument.Parse(rawPayload);
            return doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private sealed class ReplayState
    {
        public SubscriptionPlan? Plan { get; set; }

        public decimal PlanPrice { get; set; }

        public bool CancelAtPeriodEnd { get; set; }

        public SubscriptionPlan? ScheduledPlan { get; set; }

        public decimal CommittedMrr { get; set; }
    }
}
