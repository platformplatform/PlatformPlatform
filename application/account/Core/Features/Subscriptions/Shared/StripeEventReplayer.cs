using System.Text.Json;
using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using Account.Integrations.Stripe;
using SharedKernel.Domain;

namespace Account.Features.Subscriptions.Shared;

/// <summary>
///     Classifies Stripe events into BillingEvent rows under a strict 1:1 invariant: every recognized
///     subscription-relevant Stripe event yields exactly one row.
///     The hot path drives this classifier from Stripe's events.list response plus the single just-acked
///     webhook payload threaded in-memory via <c>EmitBillingEventsFromEventsListAsync</c>; the durable
///     <c>stripe_events.payload</c> column is read only by the
///     <c>ReplayArchivedTenantStripeEventsCommand</c> disaster-recovery handler, which feeds the archived
///     payloads into this same classifier when events have aged past Stripe's 30-day events.list
///     retention window.
///     Events that don't move state we care about are emitted as <see cref="BillingEventType.NoOp" />.
///     Events whose payload combines multiple state changes that don't decompose into one of our domain
///     transitions are emitted as <see cref="BillingEventType.Unclassified" /> and flip the
///     <see cref="ReplayState.HasUnclassifiedEvent" /> flag for the caller to translate into a
///     <c>Subscription.HasDriftDetected</c> change.
///     The replayer is a state machine: it iterates events in chronological order and tracks running
///     subscription state (current plan/price, cancel-at-period-end, scheduled downgrade plan, committed
///     MRR). The committed_mrr column on every row is the state-after, denormalized so paginated reads
///     don't have to walk history.
/// </summary>
public static class StripeEventReplayer
{
    public static IReadOnlyList<BillingEvent> Replay(
        Subscription subscription,
        StripeReplayEvent[] stripeEvents,
        IReadOnlyDictionary<string, SubscriptionPlan> planByPriceId,
        IReadOnlyDictionary<SubscriptionPlan, decimal> priceByPlan,
        ReplayState? state = null,
        string? currencyOverride = null,
        ILogger? logger = null
    )
    {
        var emitted = new List<BillingEvent>();
        state ??= new ReplayState();

        foreach (var stripeEvent in stripeEvents.OrderBy(e => e.CreatedAt).ThenBy(e => e.EventId))
        {
            var payload = ParsePayload(stripeEvent.Payload);

            // Per-event resolution preserves the true currency of every row even when state mutations
            // (e.g. Subscription.ResetToFreePlan in the same sync transaction) have nulled the live
            // CurrentPriceCurrency before this replayer reads it. Order: payload (authoritative for the
            // event itself), then caller-supplied override (typically a pre-mutation snapshot), then the
            // live subscription. No "USD" fallback — a missing currency is a real upstream defect.
            var currency = ExtractCurrencyFromPayload(payload)
                           ?? currencyOverride
                           ?? subscription.CurrentPriceCurrency;

            // Customer-lifecycle events (customer.created/updated and payment_method.attached) carry no
            // currency in Stripe's data model — currency belongs to prices and invoices, not the Customer
            // or PaymentMethod object. For these events, null currency is the correct semantic; for
            // revenue-bearing events, null currency is an upstream defect and the row is skipped.
            if (currency is null && RequiresCurrency(stripeEvent.EventType))
            {
                logger?.LogWarning(
                    "Skipping Stripe event {EventId} ({EventType}) for subscription '{SubscriptionId}': no currency could be resolved from payload, override, or subscription",
                    stripeEvent.EventId, stripeEvent.EventType, subscription.Id.Value
                );
                continue;
            }

            var billingEvent = MapEvent(stripeEvent, payload, subscription, state, planByPriceId, priceByPlan, currency);
            if (billingEvent is not null)
            {
                emitted.Add(billingEvent);
            }
        }

        return emitted;
    }

    private static bool RequiresCurrency(string eventType)
    {
        return eventType switch
        {
            "customer.created" or "customer.updated" or "payment_method.attached" => false,
            _ => true
        };
    }

    private static BillingEvent? MapEvent(
        StripeReplayEvent stripeEvent,
        JsonElement payload,
        Subscription subscription,
        ReplayState state,
        IReadOnlyDictionary<string, SubscriptionPlan> planByPriceId,
        IReadOnlyDictionary<SubscriptionPlan, decimal> priceByPlan,
        string? currency
    )
    {
        var subscriptionId = subscription.Id;
        var tenantId = subscription.TenantId;
        var occurredAt = stripeEvent.CreatedAt;
        var stripeEventId = stripeEvent.EventId;

        switch (stripeEvent.EventType)
        {
            case "customer.created":
                return BillingEvent.Create(
                    tenantId, subscriptionId, stripeEventId, BillingEventType.BillingInfoAdded, occurredAt, state.CommittedMrr,
                    toPlan: state.Plan, currency: currency
                );

            case "customer.updated":
                return HasBillingFieldsChanged(payload)
                    ? BillingEvent.Create(
                        tenantId, subscriptionId, stripeEventId, BillingEventType.BillingInfoUpdated, occurredAt, state.CommittedMrr,
                        toPlan: state.Plan, currency: currency
                    )
                    : NoOp(tenantId, subscriptionId, stripeEventId, occurredAt, state, currency);

            case "payment_method.attached":
                return BillingEvent.Create(
                    tenantId, subscriptionId, stripeEventId, BillingEventType.PaymentMethodUpdated, occurredAt, state.CommittedMrr,
                    toPlan: state.Plan, currency: currency
                );

            // From here on, every event type is a revenue-bearing event for which the Replay-loop's
            // `RequiresCurrency` gate guarantees a non-null currency before calling MapEvent. `currency!`
            // reflects that invariant — it is never null in these branches.
            case "customer.subscription.created":
                return MapSubscriptionCreated(payload, occurredAt, stripeEventId, tenantId, subscriptionId, state, planByPriceId, priceByPlan, currency!, subscription.Plan);

            case "customer.subscription.updated":
                return MapSubscriptionUpdated(payload, occurredAt, stripeEventId, tenantId, subscriptionId, state, planByPriceId, priceByPlan, currency!, subscription.CancellationReason);

            // customer.subscription.pending_update_applied fires alongside customer.subscription.updated for
            // the same upgrade transition. The updated event carries previous_attributes and is the higher-
            // fidelity source — pending_update_applied is recorded as NoOp to preserve the 1:1 audit row.
            case "customer.subscription.pending_update_applied":
            case "customer.subscription.pending_update_expired":
                return NoOp(tenantId, subscriptionId, stripeEventId, occurredAt, state, currency!);

            case "customer.subscription.deleted":
                return MapSubscriptionDeleted(payload, occurredAt, stripeEventId, tenantId, subscriptionId, state, currency!);

            case "customer.deleted":
                return MapCustomerDeleted(occurredAt, stripeEventId, tenantId, subscriptionId, state, currency!);

            // subscription_schedule.created carries only the current phase — the future-phase plan that
            // defines the downgrade target only shows up in the subsequent subscription_schedule.updated
            // event. The created row is preserved as NoOp for the audit trail.
            case "subscription_schedule.created":
                return NoOp(tenantId, subscriptionId, stripeEventId, occurredAt, state, currency!);

            case "subscription_schedule.updated":
                return MapScheduleUpdated(payload, occurredAt, stripeEventId, tenantId, subscriptionId, state, planByPriceId, priceByPlan, currency!);

            case "subscription_schedule.released":
            case "subscription_schedule.canceled":
                return MapScheduleTerminated(occurredAt, stripeEventId, tenantId, subscriptionId, state, currency!);

            case "invoice.payment_succeeded":
                return MapInvoicePaymentSucceeded(payload, occurredAt, stripeEventId, tenantId, subscriptionId, state, currency!);

            case "invoice.payment_failed":
                return MapInvoicePaymentFailed(payload, occurredAt, stripeEventId, tenantId, subscriptionId, state, currency!);

            case "charge.refunded":
                return BillingEvent.Create(
                    tenantId, subscriptionId, stripeEventId, BillingEventType.PaymentRefunded, occurredAt, state.CommittedMrr,
                    toPlan: state.Plan, newAmount: state.CommittedMrr, currency: currency
                );

            default:
                // Stripe event we don't have a case for. The 1:1 invariant only applies to events the
                // writer recognizes — unknown events are not subscription-relevant and are skipped.
                return null;
        }
    }

    private static BillingEvent MapSubscriptionCreated(
        JsonElement payload,
        DateTimeOffset occurredAt,
        string stripeEventId,
        TenantId tenantId,
        SubscriptionId subscriptionId,
        ReplayState state,
        IReadOnlyDictionary<string, SubscriptionPlan> planByPriceId,
        IReadOnlyDictionary<SubscriptionPlan, decimal> priceByPlan,
        string currency,
        SubscriptionPlan fallbackPlan
    )
    {
        var newPlan = ResolvePlanFromSubscriptionPayload(payload, planByPriceId) ?? fallbackPlan;
        var newPrice = ExtractUnitAmountFromPayload(payload) ?? (priceByPlan.TryGetValue(newPlan, out var catalogPrice) ? catalogPrice : 0m);
        var previousMrr = state.CommittedMrr;
        state.Plan = newPlan;
        state.PlanPrice = newPrice;
        state.CancelAtPeriodEnd = false;
        state.ScheduledPlan = null;
        state.CommittedMrr = newPrice;
        return BillingEvent.Create(
            tenantId, subscriptionId, stripeEventId, BillingEventType.SubscriptionCreated, occurredAt, state.CommittedMrr,
            toPlan: newPlan,
            previousAmount: previousMrr, newAmount: newPrice,
            amountDelta: newPrice - previousMrr,
            currency: currency
        );
    }

    private static BillingEvent MapSubscriptionUpdated(
        JsonElement payload,
        DateTimeOffset occurredAt,
        string stripeEventId,
        TenantId tenantId,
        SubscriptionId subscriptionId,
        ReplayState state,
        IReadOnlyDictionary<string, SubscriptionPlan> planByPriceId,
        IReadOnlyDictionary<SubscriptionPlan, decimal> priceByPlan,
        string currency,
        CancellationReason? subscriptionCancellationReason
    )
    {
        if (payload.ValueKind != JsonValueKind.Object)
        {
            return NoOp(tenantId, subscriptionId, stripeEventId, occurredAt, state, currency);
        }

        var previous = payload.TryGetProperty("data", out var data) && data.TryGetProperty("previous_attributes", out var previousAttributes) ? previousAttributes : default;
        if (previous.ValueKind != JsonValueKind.Object)
        {
            return NoOp(tenantId, subscriptionId, stripeEventId, occurredAt, state, currency);
        }

        var cancelAtPeriodEndChanged = previous.TryGetProperty("cancel_at_period_end", out var previousCancelAtPeriodEnd) && previousCancelAtPeriodEnd.ValueKind is JsonValueKind.True or JsonValueKind.False;
        var newPlan = ResolvePlanFromSubscriptionPayload(payload, planByPriceId);
        var previousPlan = ResolvePlanFromPreviousAttributes(previous, planByPriceId);
        var planChanged = newPlan is not null && previousPlan is not null && newPlan != previousPlan;

        // Combined cancel-toggle and plan-change in the same Stripe event payload. Our domain models these
        // as separate transitions, so we can't decompose this into one row without losing information.
        // Emit Unclassified, flip the drift flag for admin review, and don't mutate state — the next sync's
        // direct subscription-state diff against Stripe will reconcile.
        if (cancelAtPeriodEndChanged && planChanged)
        {
            state.HasUnclassifiedEvent = true;
            return BillingEvent.Create(
                tenantId, subscriptionId, stripeEventId, BillingEventType.Unclassified, occurredAt, state.CommittedMrr,
                toPlan: state.Plan, currency: currency
            );
        }

        if (cancelAtPeriodEndChanged && previousCancelAtPeriodEnd.ValueKind == JsonValueKind.False)
        {
            // false → true: cancellation scheduled. Forward MRR drops at the moment the customer commits to
            // leaving, not at the effective period end — committed MRR is the leading indicator we want.
            var previousMrr = state.CommittedMrr;
            state.CancelAtPeriodEnd = true;
            state.CommittedMrr = 0m;
            return BillingEvent.Create(
                tenantId, subscriptionId, stripeEventId, BillingEventType.SubscriptionCancelled, occurredAt, state.CommittedMrr,
                toPlan: state.Plan,
                previousAmount: previousMrr, newAmount: 0m, amountDelta: -previousMrr,
                currency: currency,
                cancellationReason: subscriptionCancellationReason
            );
        }

        if (cancelAtPeriodEndChanged && previousCancelAtPeriodEnd.ValueKind == JsonValueKind.True)
        {
            // true → false: reactivation. Restore committed MRR to the active plan's price.
            var previousMrr = state.CommittedMrr;
            state.CancelAtPeriodEnd = false;
            state.CommittedMrr = state.PlanPrice;
            return BillingEvent.Create(
                tenantId, subscriptionId, stripeEventId, BillingEventType.SubscriptionReactivated, occurredAt, state.CommittedMrr,
                toPlan: state.Plan,
                previousAmount: previousMrr, newAmount: state.CommittedMrr,
                amountDelta: state.CommittedMrr - previousMrr,
                currency: currency
            );
        }

        if (planChanged)
        {
            // Plan change (items.data[0].price changed). MRR impact is the real price diff between plans
            // looked up from the catalog — so an upgrade Standard→Premium shows +150 and a downgrade
            // Premium→Standard shows -150.
            var eventType = newPlan!.Value > previousPlan!.Value ? BillingEventType.SubscriptionUpgraded : BillingEventType.SubscriptionDowngraded;
            var previousMrr = state.CommittedMrr;
            var newPrice = ExtractUnitAmountFromPayload(payload) ?? (priceByPlan.TryGetValue(newPlan.Value, out var catalogPrice) ? catalogPrice : 0m);
            state.Plan = newPlan;
            state.PlanPrice = newPrice;
            state.CommittedMrr = state.CancelAtPeriodEnd ? 0m : newPrice;
            return BillingEvent.Create(
                tenantId, subscriptionId, stripeEventId, eventType, occurredAt, state.CommittedMrr,
                previousPlan, newPlan,
                previousMrr, state.CommittedMrr,
                state.CommittedMrr - previousMrr,
                currency
            );
        }

        // status: active → past_due (or unpaid). Customer's payment failed; subscription remains on plan but is
        // delinquent. Both Stripe statuses indicate the same business state from our perspective — the
        // dunning escalation path (past_due → unpaid → canceled) is a Stripe-side detail. Pairs with the
        // PaymentFailed row that the invoice.payment_failed event produces at the same timestamp; both rows
        // describe different facets of the same business event. Committed MRR unchanged.
        if (previous.TryGetProperty("status", out var prevStatus) && prevStatus.GetString() == "active")
        {
            var newStatus = ResolveSubscriptionStatus(payload);
            if (newStatus is "past_due" or "unpaid")
            {
                return BillingEvent.Create(
                    tenantId, subscriptionId, stripeEventId, BillingEventType.SubscriptionPastDue, occurredAt, state.CommittedMrr,
                    toPlan: state.Plan, newAmount: state.CommittedMrr, currency: currency
                );
            }
        }

        // latest_invoice rolled to a new invoice id — Stripe started a new billing cycle. This branch is
        // intentionally a NoOp:
        //   * Happy-path renewal: invoice.payment_succeeded fires next and emits SubscriptionRenewed (or
        //     PaymentRecovered for retry success). Emitting SubscriptionRenewed here too would duplicate it.
        //   * Past_due renewal: the active → past_due status change in the same payload is handled by the
        //     branch above, which emits SubscriptionPastDue. The latest_invoice change is the same business
        //     event from a different angle and adds no unique signal.
        // Audit row preserved so the 1:1 invariant holds.
        if (previous.TryGetProperty("latest_invoice", out _))
        {
            return NoOp(tenantId, subscriptionId, stripeEventId, occurredAt, state, currency);
        }

        // previous_attributes carried fields we don't track (e.g. metadata, period dates). Audit row.
        return NoOp(tenantId, subscriptionId, stripeEventId, occurredAt, state, currency);
    }

    private static string? ResolveSubscriptionStatus(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object) return null;
        var data = payload.TryGetProperty("data", out var dataElement) ? dataElement : default;
        if (data.ValueKind != JsonValueKind.Object) return null;
        var subscription = data.TryGetProperty("object", out var subscriptionObject) ? subscriptionObject : default;
        if (subscription.ValueKind != JsonValueKind.Object) return null;
        return subscription.TryGetProperty("status", out var status) ? status.GetString() : null;
    }

    private static BillingEvent MapSubscriptionDeleted(
        JsonElement payload,
        DateTimeOffset occurredAt,
        string stripeEventId,
        TenantId tenantId,
        SubscriptionId subscriptionId,
        ReplayState state,
        string currency
    )
    {
        // Stripe clears cancel_at_period_end BEFORE emitting customer.subscription.deleted, so the payload's
        // cape field is unreliable for distinguishing a voluntary period-end expiry from an immediate cancel.
        // The leading signal is cancellation_details.reason (payment_failed → dunning, cancellation_requested
        // → voluntary), with state.CancelAtPeriodEnd (tracked from prior subscription.updated events) as the
        // disambiguator for voluntary cases.
        var cancellationReason = ExtractCancellationReasonFromPayload(payload);

        BillingEventType eventType;
        SuspensionReason? suspensionReason = null;
        if (cancellationReason == "payment_failed")
        {
            eventType = BillingEventType.SubscriptionSuspended;
            suspensionReason = SuspensionReason.PaymentFailed;
        }
        else if (state.CancelAtPeriodEnd)
        {
            eventType = BillingEventType.SubscriptionExpired;
        }
        else
        {
            eventType = BillingEventType.SubscriptionImmediatelyCancelled;
        }

        var previousMrr = state.CommittedMrr;
        var fromPlan = state.Plan;
        state.Plan = null;
        state.PlanPrice = 0m;
        state.CancelAtPeriodEnd = false;
        state.ScheduledPlan = null;
        state.CommittedMrr = 0m;
        return BillingEvent.Create(
            tenantId, subscriptionId, stripeEventId, eventType, occurredAt, state.CommittedMrr,
            fromPlan, SubscriptionPlan.Basis,
            previousMrr, 0m, -previousMrr,
            currency,
            suspensionReason: suspensionReason
        );
    }

    private static BillingEvent MapCustomerDeleted(
        DateTimeOffset occurredAt,
        string stripeEventId,
        TenantId tenantId,
        SubscriptionId subscriptionId,
        ReplayState state,
        string currency
    )
    {
        // Stripe customer deletion zeroes the tenant's MRR — emitted as SubscriptionSuspended with the
        // CustomerDeleted reason so the audit log captures why the subscription ended even when the
        // corresponding customer.subscription.deleted event never arrived (or arrives separately).
        var previousMrr = state.CommittedMrr;
        var fromPlan = state.Plan;
        state.Plan = null;
        state.PlanPrice = 0m;
        state.CancelAtPeriodEnd = false;
        state.ScheduledPlan = null;
        state.CommittedMrr = 0m;
        return BillingEvent.Create(
            tenantId, subscriptionId, stripeEventId, BillingEventType.SubscriptionSuspended, occurredAt, state.CommittedMrr,
            fromPlan, SubscriptionPlan.Basis,
            previousMrr, 0m, -previousMrr,
            currency,
            suspensionReason: SuspensionReason.CustomerDeleted
        );
    }

    private static BillingEvent MapScheduleUpdated(
        JsonElement payload,
        DateTimeOffset occurredAt,
        string stripeEventId,
        TenantId tenantId,
        SubscriptionId subscriptionId,
        ReplayState state,
        IReadOnlyDictionary<string, SubscriptionPlan> planByPriceId,
        IReadOnlyDictionary<SubscriptionPlan, decimal> priceByPlan,
        string currency
    )
    {
        // Stripe emits a trailing schedule.updated event with status=canceled/released/completed right
        // after a schedule is dropped; the phases array hasn't changed, so falling through to the
        // resolver would re-emit a phantom DowngradeScheduled. Terminal-status updates are NoOp.
        var status = ResolveScheduleStatus(payload);
        if (status is "canceled" or "released" or "completed")
        {
            return NoOp(tenantId, subscriptionId, stripeEventId, occurredAt, state, currency);
        }

        var scheduledPlan = ResolveScheduledTargetPlan(payload, planByPriceId, state.Plan);
        if (scheduledPlan is null || scheduledPlan == state.ScheduledPlan)
        {
            return NoOp(tenantId, subscriptionId, stripeEventId, occurredAt, state, currency);
        }

        var scheduledPrice = ExtractUnitAmountFromPayload(payload) ?? (priceByPlan.TryGetValue(scheduledPlan.Value, out var catalogPrice) ? catalogPrice : 0m);
        var previousMrr = state.CommittedMrr;
        state.ScheduledPlan = scheduledPlan;
        state.CommittedMrr = scheduledPrice;
        return BillingEvent.Create(
            tenantId, subscriptionId, stripeEventId, BillingEventType.SubscriptionDowngradeScheduled, occurredAt, state.CommittedMrr,
            state.Plan, scheduledPlan,
            previousMrr, scheduledPrice,
            scheduledPrice - previousMrr,
            currency
        );
    }

    private static BillingEvent MapScheduleTerminated(
        DateTimeOffset occurredAt,
        string stripeEventId,
        TenantId tenantId,
        SubscriptionId subscriptionId,
        ReplayState state,
        string currency
    )
    {
        if (state.ScheduledPlan is null)
        {
            // Schedule terminated without ever having a scheduled plan tracked locally — possibly because
            // we missed the corresponding subscription_schedule.updated. Audit row.
            return NoOp(tenantId, subscriptionId, stripeEventId, occurredAt, state, currency);
        }

        var previousMrr = state.CommittedMrr;
        var newMrr = state.PlanPrice;
        state.ScheduledPlan = null;
        state.CommittedMrr = newMrr;
        var delta = newMrr - previousMrr;
        return BillingEvent.Create(
            tenantId, subscriptionId, stripeEventId, BillingEventType.SubscriptionDowngradeCancelled, occurredAt, state.CommittedMrr,
            toPlan: state.Plan,
            previousAmount: previousMrr, newAmount: newMrr,
            amountDelta: delta == 0m ? null : delta,
            currency: currency
        );
    }

    private static BillingEvent MapInvoicePaymentSucceeded(
        JsonElement payload,
        DateTimeOffset occurredAt,
        string stripeEventId,
        TenantId tenantId,
        SubscriptionId subscriptionId,
        ReplayState state,
        string currency
    )
    {
        // Only emit a Renewed/Recovered row for genuine recurring renewals (billing_reason ==
        // subscription_cycle). subscription_create is covered by customer.subscription.created;
        // subscription_update is the proration invoice from a plan change and is covered by the
        // customer.subscription.updated upgrade/downgrade row — emitting Renewed here would duplicate it.
        var billingReason = ExtractInvoiceBillingReason(payload);
        if (billingReason != "subscription_cycle")
        {
            return NoOp(tenantId, subscriptionId, stripeEventId, occurredAt, state, currency);
        }

        var eventType = HasMultiplePaymentAttempts(payload) ? BillingEventType.PaymentRecovered : BillingEventType.SubscriptionRenewed;
        return BillingEvent.Create(
            tenantId, subscriptionId, stripeEventId, eventType, occurredAt, state.CommittedMrr,
            toPlan: state.Plan,
            newAmount: state.CommittedMrr,
            currency: currency
        );
    }

    private static BillingEvent MapInvoicePaymentFailed(
        JsonElement payload,
        DateTimeOffset occurredAt,
        string stripeEventId,
        TenantId tenantId,
        SubscriptionId subscriptionId,
        ReplayState state,
        string currency
    )
    {
        // Only emit PaymentFailed for genuine recurring billing failures (billing_reason ==
        // subscription_cycle). The proration invoice from a plan change can also fail and is covered by
        // the customer.subscription.updated upgrade/downgrade row instead. Failures don't change committed
        // MRR — the customer is still on the plan, just behind on payment. If Stripe later succeeds via a
        // retry (3DS or smart-retry), invoice.payment_succeeded fires and emits PaymentRecovered — that's
        // accurate history rather than swallowing the initial failure.
        var billingReason = ExtractInvoiceBillingReason(payload);
        if (billingReason != "subscription_cycle")
        {
            return NoOp(tenantId, subscriptionId, stripeEventId, occurredAt, state, currency);
        }

        return BillingEvent.Create(
            tenantId, subscriptionId, stripeEventId, BillingEventType.PaymentFailed, occurredAt, state.CommittedMrr,
            toPlan: state.Plan,
            newAmount: state.CommittedMrr,
            currency: currency
        );
    }

    // NoOp accepts nullable currency because the customer.updated branch reaches NoOp via the
    // RequiresCurrency-bypass path (customer-lifecycle events have no currency in Stripe's data model);
    // all other NoOp call sites are revenue contexts where currency is non-null.
    private static BillingEvent NoOp(TenantId tenantId, SubscriptionId subscriptionId, string stripeEventId, DateTimeOffset occurredAt, ReplayState state, string? currency)
    {
        return BillingEvent.Create(
            tenantId, subscriptionId, stripeEventId, BillingEventType.NoOp, occurredAt, state.CommittedMrr,
            toPlan: state.Plan, currency: currency
        );
    }

    private static SubscriptionPlan? ResolvePlanFromSubscriptionPayload(JsonElement payload, IReadOnlyDictionary<string, SubscriptionPlan> planByPriceId)
    {
        if (payload.ValueKind != JsonValueKind.Object) return null;
        var data = payload.TryGetProperty("data", out var dataElement) ? dataElement : default;
        if (data.ValueKind != JsonValueKind.Object) return null;
        var subscription = data.TryGetProperty("object", out var subscriptionObject) ? subscriptionObject : default;
        if (subscription.ValueKind != JsonValueKind.Object) return null;
        var items = subscription.TryGetProperty("items", out var itemsElement) ? itemsElement : default;
        if (items.ValueKind != JsonValueKind.Object) return null;
        var itemsData = items.TryGetProperty("data", out var itemsDataElement) ? itemsDataElement : default;
        if (itemsData.ValueKind != JsonValueKind.Array) return null;
        foreach (var item in itemsData.EnumerateArray())
        {
            var priceId = item.TryGetProperty("price", out var price) && price.TryGetProperty("id", out var priceIdElement) ? priceIdElement.GetString() : null;
            if (priceId is not null && planByPriceId.TryGetValue(priceId, out var plan)) return plan;
        }

        return null;
    }

    private static SubscriptionPlan? ResolvePlanFromPreviousAttributes(JsonElement previousAttributes, IReadOnlyDictionary<string, SubscriptionPlan> planByPriceId)
    {
        if (previousAttributes.ValueKind != JsonValueKind.Object) return null;
        if (!previousAttributes.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Object) return null;
        if (!items.TryGetProperty("data", out var itemsData) || itemsData.ValueKind != JsonValueKind.Array) return null;
        foreach (var item in itemsData.EnumerateArray())
        {
            var priceId = item.TryGetProperty("price", out var price) && price.TryGetProperty("id", out var priceIdElement) ? priceIdElement.GetString() : null;
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
        if (payload.ValueKind != JsonValueKind.Object) return null;
        var data = payload.TryGetProperty("data", out var dataElement) ? dataElement : default;
        if (data.ValueKind != JsonValueKind.Object) return null;
        var schedule = data.TryGetProperty("object", out var scheduleObject) ? scheduleObject : default;
        if (schedule.ValueKind != JsonValueKind.Object) return null;
        var phases = schedule.TryGetProperty("phases", out var phasesElement) ? phasesElement : default;
        if (phases.ValueKind != JsonValueKind.Array) return null;

        SubscriptionPlan? lastPhasePlan = null;
        var phaseCount = 0;
        foreach (var phase in phases.EnumerateArray())
        {
            phaseCount++;
            var items = phase.TryGetProperty("items", out var itemsElement) ? itemsElement : default;
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

    private static string? ResolveScheduleStatus(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object) return null;
        var data = payload.TryGetProperty("data", out var dataElement) ? dataElement : default;
        if (data.ValueKind != JsonValueKind.Object) return null;
        var schedule = data.TryGetProperty("object", out var scheduleObject) ? scheduleObject : default;
        if (schedule.ValueKind != JsonValueKind.Object) return null;
        return schedule.TryGetProperty("status", out var status) ? status.GetString() : null;
    }

    private static bool HasBillingFieldsChanged(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object) return false;
        var previous = payload.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object && data.TryGetProperty("previous_attributes", out var previousAttributes) ? previousAttributes : default;
        if (previous.ValueKind != JsonValueKind.Object) return false;
        return previous.TryGetProperty("address", out _)
               || previous.TryGetProperty("email", out _)
               || previous.TryGetProperty("name", out _)
               || previous.TryGetProperty("tax_ids", out _);
    }

    private static string? ExtractInvoiceBillingReason(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object) return null;
        var data = payload.TryGetProperty("data", out var dataElement) ? dataElement : default;
        if (data.ValueKind != JsonValueKind.Object) return null;
        var invoice = data.TryGetProperty("object", out var invoiceObject) ? invoiceObject : default;
        if (invoice.ValueKind != JsonValueKind.Object) return null;
        return invoice.TryGetProperty("billing_reason", out var billingReason) ? billingReason.GetString() : null;
    }

    private static bool HasMultiplePaymentAttempts(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object) return false;
        var data = payload.TryGetProperty("data", out var dataElement) ? dataElement : default;
        if (data.ValueKind != JsonValueKind.Object) return false;
        var invoice = data.TryGetProperty("object", out var invoiceObject) ? invoiceObject : default;
        if (invoice.ValueKind != JsonValueKind.Object) return false;
        var attemptCount = invoice.TryGetProperty("attempt_count", out var attemptCountElement) && attemptCountElement.ValueKind == JsonValueKind.Number ? attemptCountElement.GetInt32() : 0;
        return attemptCount > 1;
    }

    /// <summary>
    ///     Extracts the ISO 4217 currency code from a Stripe event payload. invoice.* and
    ///     customer.subscription.* objects expose <c>$.data.object.currency</c>; the subscription object
    ///     additionally carries the per-item price under <c>$.data.object.items.data[0].price.currency</c>
    ///     which is the authoritative source on the rare event types where the top-level <c>currency</c>
    ///     is absent. Stripe normalizes currency codes to lowercase in payloads (per
    ///     https://docs.stripe.com/currencies); we upper-case to match the rest of the codebase.
    /// </summary>
    private static string? ExtractCurrencyFromPayload(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object) return null;
        var data = payload.TryGetProperty("data", out var dataElement) ? dataElement : default;
        if (data.ValueKind != JsonValueKind.Object) return null;
        var stripeObject = data.TryGetProperty("object", out var rawObject) ? rawObject : default;
        if (stripeObject.ValueKind != JsonValueKind.Object) return null;

        if (stripeObject.TryGetProperty("currency", out var currency) && currency.ValueKind == JsonValueKind.String)
        {
            return currency.GetString()?.ToUpperInvariant();
        }

        if (stripeObject.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Object
                                                                && items.TryGetProperty("data", out var itemsData) && itemsData.ValueKind == JsonValueKind.Array
                                                                && itemsData.GetArrayLength() > 0)
        {
            var firstItem = itemsData[0];
            if (firstItem.ValueKind == JsonValueKind.Object
                && firstItem.TryGetProperty("price", out var price) && price.ValueKind == JsonValueKind.Object
                && price.TryGetProperty("currency", out var priceCurrency) && priceCurrency.ValueKind == JsonValueKind.String)
            {
                return priceCurrency.GetString()?.ToUpperInvariant();
            }
        }

        return null;
    }

    /// <summary>
    ///     Extracts <c>$.data.object.items.data[0].price.unit_amount</c> from a Stripe subscription event
    ///     payload. This is the locked-in price for this specific subscription at the time the event fired,
    ///     which is authoritative over the live catalog <c>priceByPlan</c> dictionary — an admin who archives
    ///     a plan's price and creates a new active one for the same plan would otherwise cause the catalog
    ///     lookup to return the new price while the subscription remains on the old locked-in price.
    ///     Stripe encodes <c>unit_amount</c> as a long in minor units (cents); the value is divided by 100
    ///     to produce the major-unit decimal the rest of the domain uses. Returns null when the payload
    ///     doesn't carry a unit_amount (e.g., subscription_schedule events whose price lives elsewhere), in
    ///     which case the caller falls back to the catalog dictionary.
    /// </summary>
    private static decimal? ExtractUnitAmountFromPayload(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object) return null;
        var data = payload.TryGetProperty("data", out var dataElement) ? dataElement : default;
        if (data.ValueKind != JsonValueKind.Object) return null;
        var stripeObject = data.TryGetProperty("object", out var rawObject) ? rawObject : default;
        if (stripeObject.ValueKind != JsonValueKind.Object) return null;
        if (!stripeObject.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Object) return null;
        if (!items.TryGetProperty("data", out var itemsData) || itemsData.ValueKind != JsonValueKind.Array || itemsData.GetArrayLength() == 0) return null;
        var firstItem = itemsData[0];
        if (firstItem.ValueKind != JsonValueKind.Object) return null;
        if (!firstItem.TryGetProperty("price", out var price) || price.ValueKind != JsonValueKind.Object) return null;
        if (!price.TryGetProperty("unit_amount", out var unitAmount) || unitAmount.ValueKind != JsonValueKind.Number) return null;
        return unitAmount.GetInt64() / 100m;
    }

    /// <summary>
    ///     Extracts <c>$.data.object.cancellation_details.reason</c> from a Stripe subscription event payload.
    ///     Stripe populates this on customer.subscription.deleted to convey *why* the subscription ended —
    ///     <c>payment_failed</c> for dunning-driven terminations, <c>cancellation_requested</c> for voluntary
    ///     cancels. Returns the lowercase string as Stripe emits it, or null when absent or non-string.
    /// </summary>
    private static string? ExtractCancellationReasonFromPayload(JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object) return null;
        var data = payload.TryGetProperty("data", out var dataElement) ? dataElement : default;
        if (data.ValueKind != JsonValueKind.Object) return null;
        var stripeObject = data.TryGetProperty("object", out var rawObject) ? rawObject : default;
        if (stripeObject.ValueKind != JsonValueKind.Object) return null;
        var cancellationDetails = stripeObject.TryGetProperty("cancellation_details", out var details) ? details : default;
        if (cancellationDetails.ValueKind != JsonValueKind.Object) return null;
        if (!cancellationDetails.TryGetProperty("reason", out var reason) || reason.ValueKind != JsonValueKind.String) return null;
        return reason.GetString()?.ToLowerInvariant();
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

    /// <summary>
    ///     Seeds a fresh <see cref="ReplayState" /> for the next replay batch. Current-state-of-the-world
    ///     fields (Plan, PlanPrice, ScheduledPlan, CancelAtPeriodEnd) come from the live
    ///     <paramref name="subscription" /> aggregate — which was just reconciled from Stripe earlier in the
    ///     same sync — so the replayer's view of the active plan stays consistent with truth even when a
    ///     transition (e.g. subscription_schedule.released cancelling a pending downgrade) doesn't emit a
    ///     paired customer.subscription.updated to refresh history. The running total
    ///     (<see cref="ReplayState.CommittedMrr" />) comes from <paramref name="latestPersisted" /> so the
    ///     anchor doesn't reset to zero when events.list re-pulls a partial window. Returns a fresh state
    ///     with CommittedMrr=0 when <paramref name="latestPersisted" /> is null.
    /// </summary>
    public static ReplayState SeedReplayStateFromHistory(BillingEvent? latestPersisted, Subscription subscription)
    {
        // When the most recently persisted row is a SubscriptionDowngradeScheduled, history shows a still-
        // open schedule that the live aggregate may have already discarded. Stripe's
        // subscription_schedule.released cancels the schedule without re-emitting
        // customer.subscription.updated, so SyncStateFromStripe nulls ScheduledPlan on the live aggregate
        // before this seed runs. Falling back to history here lets MapScheduleTerminated classify the
        // cancellation as SubscriptionDowngradeCancelled instead of a NoOp audit row.
        var scheduledPlan = latestPersisted?.EventType == BillingEventType.SubscriptionDowngradeScheduled
            ? latestPersisted.ToPlan
            : subscription.ScheduledPlan;

        return new ReplayState
        {
            Plan = subscription.Plan,
            PlanPrice = subscription.CurrentPriceAmount ?? 0m,
            ScheduledPlan = scheduledPlan,
            CancelAtPeriodEnd = subscription.CancelAtPeriodEnd,
            CommittedMrr = latestPersisted?.CommittedMrr ?? 0m
        };
    }

    public sealed class ReplayState
    {
        public SubscriptionPlan? Plan { get; set; }

        public decimal PlanPrice { get; set; }

        public bool CancelAtPeriodEnd { get; set; }

        public SubscriptionPlan? ScheduledPlan { get; set; }

        public decimal CommittedMrr { get; set; }

        /// <summary>
        ///     Set to true when the replayer encounters a Stripe event whose payload combines multiple
        ///     state changes that don't decompose into a single domain transition. Callers translate this
        ///     into a <c>Subscription.SetDriftStatus</c> call so the existing drift banner picks it up.
        /// </summary>
        public bool HasUnclassifiedEvent { get; set; }
    }
}
