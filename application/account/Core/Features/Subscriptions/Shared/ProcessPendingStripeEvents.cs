using System.Data;
using Account.Database;
using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using Account.Integrations.Stripe;
using Microsoft.ApplicationInsights;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Telemetry;

namespace Account.Features.Subscriptions.Shared;

/// <summary>
///     Phase 2 of two-phase webhook processing. Acquires a pessimistic lock on the subscription row to
///     serialize concurrent webhook processing, syncs current state from Stripe, then writes new
///     BillingEvent rows from the events.list view of the world. The hot path NEVER reads
///     <c>stripe_events.payload</c>: it drives BillingEvent emission from Stripe's events.list response
///     (anchored on the subscription's <see cref="Subscription.LastSyncedStripeEventCreatedAt" />) and
///     from the just-arrived webhook payloads carried by the in-memory <c>pendingEvents</c> set. The
///     local <c>stripe_events</c> archive stays as a cold backup; only the admin "Reconcile with Stripe"
///     action walks it (see <see cref="StripeEventReplayer" />). The unique <c>stripe_event_id</c> index
///     on <c>billing_events</c> makes the emission idempotent: redelivered webhooks and re-pulls from
///     events.list are no-ops.
/// </summary>
public sealed class ProcessPendingStripeEvents(
    AccountDbContext dbContext,
    ISubscriptionRepository subscriptionRepository,
    IStripeEventRepository stripeEventRepository,
    IBillingEventRepository billingEventRepository,
    ITenantRepository tenantRepository,
    StripeClientFactory stripeClientFactory,
    TimeProvider timeProvider,
    ITelemetryEventsCollector events,
    TelemetryClient telemetryClient,
    ILogger<ProcessPendingStripeEvents> logger
)
{
    public Task ExecuteAsync(StripeCustomerId stripeCustomerId, CancellationToken cancellationToken)
    {
        return ExecuteAsync(stripeCustomerId, false, cancellationToken);
    }

    public async Task ExecuteAsync(StripeCustomerId stripeCustomerId, bool forceSync, CancellationToken cancellationToken)
    {
        // Pessimistic lock serializes concurrent webhook processing for the same customer
        var isSqlite = dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite";
        await using var transaction = isSqlite
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        var subscription = await subscriptionRepository.GetByStripeCustomerIdWithLockUnfilteredAsync(stripeCustomerId, cancellationToken);
        if (subscription is null)
        {
            logger.LogWarning("Subscription not found for Stripe customer '{StripeCustomerId}', events will be retried on next webhook", stripeCustomerId);
            await transaction.RollbackAsync(cancellationToken);
            return;
        }

        var tenant = (await tenantRepository.GetByIdUnfilteredAsync(subscription.TenantId, cancellationToken))!;
        var pendingEvents = await stripeEventRepository.GetPendingByStripeCustomerIdAsync(stripeCustomerId, cancellationToken);

        // forceSync runs the Stripe sync even with no pending events (used by the back-office reconcile admin action)
        if (pendingEvents.Length > 0 || forceSync)
        {
            var eventsListResults = await PullEventsListAndArchiveRecoveredAsync(subscription, stripeCustomerId, cancellationToken);
            var driftSnapshots = await SyncStateFromStripe(tenant, subscription, cancellationToken);
            await EmitBillingEventsFromEventsListAsync(subscription, pendingEvents, eventsListResults, driftSnapshots, cancellationToken);

            MarkAllEventsAsProcessed(pendingEvents, subscription);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        SendTelemetryEvents(tenant, subscription);
    }

    private async Task<DriftSnapshots> SyncStateFromStripe(Tenant tenant, Subscription subscription, CancellationToken cancellationToken)
    {
        var stripeClient = stripeClientFactory.GetClient();
        var customerResult = await stripeClient.GetCustomerBillingInfoAsync(subscription.StripeCustomerId!, cancellationToken);

        // Snapshot captured before any mutation so the drift detector can compare local-pre-sync against Stripe.
        var localSnapshot = StripeSyncSnapshot.FromSubscription(subscription);

        var previousPlan = subscription.Plan;
        var previousPriceAmount = subscription.CurrentPriceAmount;
        var previousPriceCurrency = subscription.CurrentPriceCurrency;

        if (customerResult is null)
        {
            logger.LogError("Failed to fetch billing info for Stripe customer '{StripeCustomerId}'", subscription.StripeCustomerId);
            return new DriftSnapshots(localSnapshot, null);
        }

        if (customerResult.IsCustomerDeleted)
        {
            var nowAtCustomerDeleted = timeProvider.GetUtcNow();
            subscription.ResetToFreePlan();
            tenant.UpdatePlan(SubscriptionPlan.Basis);
            tenant.Suspend(SuspensionReason.CustomerDeleted, nowAtCustomerDeleted);
            tenantRepository.Update(tenant);
            subscriptionRepository.Update(subscription);
            events.CollectEvent(new SubscriptionSuspended(subscription.Id, previousPlan, SuspensionReason.CustomerDeleted, previousPriceAmount!.Value, -previousPriceAmount.Value, previousPriceCurrency!));
            // Stripe's view: customer is gone, no subscription. Pair with the pre-sync local snapshot above.
            return new DriftSnapshots(localSnapshot, new StripeSyncSnapshot(SubscriptionPlan.Basis, false, null, null));
        }

        var stripeState = await stripeClient.SyncSubscriptionStateAsync(subscription.StripeCustomerId!, cancellationToken);

        // Detect state transitions in lifecycle order (variables and if-blocks below follow the same order).
        // The detections drive telemetry collection and Subscription/Tenant state mutations; the BillingEvent
        // log is populated separately by EmitBillingEventsFromEventsListAsync running over the events.list view.
        var billingInfoAdded = subscription.BillingInfo is null && customerResult.BillingInfo is not null;
        var billingInfoUpdated = subscription.BillingInfo is not null && customerResult.BillingInfo is not null && customerResult.BillingInfo != subscription.BillingInfo;
        var latestPaymentMethod = stripeState?.PaymentMethod ?? customerResult.PaymentMethod;
        var paymentMethodUpdated = latestPaymentMethod != subscription.PaymentMethod;
        var subscriptionCreated = subscription.StripeSubscriptionId is null && stripeState?.StripeSubscriptionId is not null;
        var subscriptionRenewed = subscription.CurrentPeriodEnd is not null && stripeState?.CurrentPeriodEnd is not null && stripeState.CurrentPeriodEnd > subscription.CurrentPeriodEnd;
        var subscriptionUpgraded = !subscriptionCreated && stripeState is not null && stripeState.Plan != subscription.Plan && stripeState.Plan.IsUpgradeFrom(subscription.Plan);
        var downgradeScheduled = subscription.ScheduledPlan is null && stripeState?.ScheduledPlan is not null;
        var downgradeCancelled = subscription.ScheduledPlan is not null && stripeState?.ScheduledPlan is null && stripeState is not null && stripeState.Plan == subscription.Plan;
        var subscriptionDowngraded = subscription.ScheduledPlan is not null && stripeState?.ScheduledPlan is null && stripeState is not null && stripeState.Plan != subscription.Plan && subscription.Plan.IsUpgradeFrom(stripeState.Plan);
        var subscriptionCancelled = !subscription.CancelAtPeriodEnd && stripeState?.CancelAtPeriodEnd == true;
        var subscriptionReactivated = subscription.CancelAtPeriodEnd && stripeState?.CancelAtPeriodEnd == false;
        var subscriptionExpired = subscription.StripeSubscriptionId is not null && stripeState is null && subscription is { CancelAtPeriodEnd: true, FirstPaymentFailedAt: null };
        var subscriptionImmediatelyCancelled = subscription.StripeSubscriptionId is not null && stripeState is null && subscription is { CancelAtPeriodEnd: false, FirstPaymentFailedAt: null };
        var subscriptionSuspended = subscription.StripeSubscriptionId is not null && stripeState is null && subscription.FirstPaymentFailedAt is not null;
        var paymentFailed = stripeState?.SubscriptionStatus is StripeSubscriptionStatus.PastDue or StripeSubscriptionStatus.Incomplete && subscription.FirstPaymentFailedAt is null;
        var paymentRecovered = stripeState?.SubscriptionStatus == StripeSubscriptionStatus.Active && subscription.FirstPaymentFailedAt is not null;
        var previousRefundCount = subscription.PaymentTransactions.Count(t => t.Status == PaymentTransactionStatus.Refunded);
        var now = timeProvider.GetUtcNow();
        var daysOnCurrentPlan = (int)(now - (subscription.ModifiedAt ?? subscription.CreatedAt)).TotalDays;

        if (stripeState is not null)
        {
            subscription.SetStripeSubscription(stripeState.StripeSubscriptionId, stripeState.Plan, stripeState.CurrentPriceAmount, stripeState.CurrentPriceCurrency, stripeState.CurrentPeriodEnd, stripeState.PaymentMethod);
            tenant.UpdatePlan(stripeState.Plan);
        }

        var syncedTransactions = stripeState?.PaymentTransactions ?? await stripeClient.SyncPaymentTransactionsAsync(subscription.StripeCustomerId!, cancellationToken);
        if (syncedTransactions is not null)
        {
            subscription.SetPaymentTransactions([.. syncedTransactions]);
        }

        var paymentRefunded = subscription.PaymentTransactions.Count(t => t.Status == PaymentTransactionStatus.Refunded) > previousRefundCount;

        if (billingInfoAdded)
        {
            subscription.SetBillingInfo(customerResult.BillingInfo);
            events.CollectEvent(new BillingInfoAdded(subscription.Id, customerResult.BillingInfo?.Address?.Country, customerResult.BillingInfo?.Address?.PostalCode, customerResult.BillingInfo?.Address?.City));
        }

        if (billingInfoUpdated)
        {
            subscription.SetBillingInfo(customerResult.BillingInfo);
            events.CollectEvent(new BillingInfoUpdated(subscription.Id, customerResult.BillingInfo?.Address?.Country, customerResult.BillingInfo?.Address?.PostalCode, customerResult.BillingInfo?.Address?.City));
        }

        if (paymentMethodUpdated)
        {
            subscription.SetPaymentMethod(latestPaymentMethod);
            events.CollectEvent(new PaymentMethodUpdated(subscription.Id));
        }

        if (subscriptionCreated)
        {
            tenant.Activate();
            events.CollectEvent(new SubscriptionCreated(subscription.Id, subscription.Plan, subscription.CurrentPriceAmount!.Value, subscription.CurrentPriceAmount!.Value, subscription.CurrentPriceCurrency!));
        }

        if (subscriptionRenewed)
        {
            events.CollectEvent(new SubscriptionRenewed(subscription.Id, subscription.Plan, subscription.CurrentPriceAmount!.Value, subscription.CurrentPriceAmount!.Value - previousPriceAmount!.Value, subscription.CurrentPriceCurrency!));
        }

        if (subscriptionUpgraded)
        {
            events.CollectEvent(new SubscriptionUpgraded(subscription.Id, previousPlan, subscription.Plan, daysOnCurrentPlan, previousPriceAmount!.Value, subscription.CurrentPriceAmount!.Value, subscription.CurrentPriceAmount!.Value - previousPriceAmount.Value, subscription.CurrentPriceCurrency!));
        }

        if (downgradeScheduled)
        {
            var priceCatalog = await stripeClient.GetPriceCatalogAsync(cancellationToken);
            var scheduledPlanPrice = priceCatalog.Single(p => p.Plan == stripeState!.ScheduledPlan!.Value).UnitAmount;
            subscription.SetScheduledPlan(stripeState!.ScheduledPlan, scheduledPlanPrice);
            var daysUntilDowngrade = subscription.CurrentPeriodEnd is not null ? (int)(subscription.CurrentPeriodEnd.Value - now).TotalDays : (int?)null;
            events.CollectEvent(new SubscriptionDowngradeScheduled(subscription.Id, subscription.Plan, subscription.ScheduledPlan!.Value, daysUntilDowngrade, subscription.CurrentPriceAmount!.Value, scheduledPlanPrice - subscription.CurrentPriceAmount!.Value, subscription.CurrentPriceCurrency!));
        }

        if (downgradeCancelled)
        {
            var previousScheduledPlan = subscription.ScheduledPlan;
            var daysSinceDowngradeScheduled = (int)(now - (subscription.ModifiedAt ?? subscription.CreatedAt)).TotalDays;
            subscription.SetScheduledPlan(stripeState!.ScheduledPlan, null);
            var daysUntilDowngrade = subscription.CurrentPeriodEnd is not null ? (int)(subscription.CurrentPeriodEnd.Value - now).TotalDays : (int?)null;
            var priceCatalog = await stripeClient.GetPriceCatalogAsync(cancellationToken);
            var scheduledPlanPrice = priceCatalog.Single(p => p.Plan == previousScheduledPlan!.Value).UnitAmount;
            events.CollectEvent(new SubscriptionDowngradeCancelled(subscription.Id, subscription.Plan, previousScheduledPlan!.Value, daysUntilDowngrade, daysSinceDowngradeScheduled, subscription.CurrentPriceAmount!.Value, subscription.CurrentPriceAmount!.Value - scheduledPlanPrice, subscription.CurrentPriceCurrency!));
        }

        if (subscriptionDowngraded)
        {
            subscription.SetScheduledPlan(stripeState!.ScheduledPlan, null);
            events.CollectEvent(new SubscriptionDowngraded(subscription.Id, previousPlan, subscription.Plan, daysOnCurrentPlan, previousPriceAmount!.Value, subscription.CurrentPriceAmount!.Value, subscription.CurrentPriceAmount!.Value - previousPriceAmount.Value, subscription.CurrentPriceCurrency!));
        }

        // Unconditional reconciliation of the scheduled-plan price from the catalog. Mirrors how
        // SetStripeSubscription above unconditionally reconciles CurrentPriceAmount on every sync. Without
        // this, a cancel-then-reschedule pair landing in the same sync window leaves both diff flags
        // (downgradeScheduled, downgradeCancelled) false — the local pre-sync ScheduledPlan equals the
        // Stripe post-sync ScheduledPlan — and scheduled_price_amount stays NULL from an earlier transition
        // (e.g. the downgradeCancelled call to SetScheduledPlan(..., null)). MrrCalculator.ForwardMrr then
        // falls back to CurrentPriceAmount, overstating BLENDED MRR. The reconciliation is idempotent:
        // when the diff detector already set the correct price, this re-applies the same value.
        if (stripeState?.ScheduledPlan is not null)
        {
            var priceCatalog = await stripeClient.GetPriceCatalogAsync(cancellationToken);
            var scheduledPlanPrice = priceCatalog.Single(p => p.Plan == stripeState.ScheduledPlan.Value).UnitAmount;
            subscription.SetScheduledPlan(stripeState.ScheduledPlan, scheduledPlanPrice);
        }

        if (subscriptionCancelled)
        {
            subscription.SetCancellation(stripeState!.CancelAtPeriodEnd, stripeState.CancellationReason, stripeState.CancellationFeedback);
            var daysUntilExpiry = subscription.CurrentPeriodEnd is not null ? (int)(subscription.CurrentPeriodEnd.Value - now).TotalDays : (int?)null;
            events.CollectEvent(new SubscriptionCancelled(subscription.Id, subscription.Plan, subscription.CancellationReason ?? CancellationReason.CancelledByAdmin, daysUntilExpiry, daysOnCurrentPlan, subscription.CurrentPriceAmount!.Value, -subscription.CurrentPriceAmount!.Value, subscription.CurrentPriceCurrency!));
        }

        if (subscriptionReactivated)
        {
            var daysSinceCancelled = (int)(now - (subscription.ModifiedAt ?? subscription.CreatedAt)).TotalDays;
            subscription.SetCancellation(stripeState!.CancelAtPeriodEnd, stripeState.CancellationReason, stripeState.CancellationFeedback);
            var daysUntilExpiry = subscription.CurrentPeriodEnd is not null ? (int)(subscription.CurrentPeriodEnd.Value - now).TotalDays : (int?)null;
            events.CollectEvent(new SubscriptionReactivated(subscription.Id, subscription.Plan, daysUntilExpiry, daysSinceCancelled, subscription.CurrentPriceAmount!.Value, subscription.CurrentPriceAmount!.Value, subscription.CurrentPriceCurrency!));
        }

        if (subscriptionExpired)
        {
            subscription.ResetToFreePlan();
            tenant.UpdatePlan(SubscriptionPlan.Basis);
            events.CollectEvent(new SubscriptionExpired(subscription.Id, previousPlan, daysOnCurrentPlan, previousPriceAmount!.Value, -previousPriceAmount.Value, previousPriceCurrency!));
        }

        if (subscriptionImmediatelyCancelled)
        {
            subscription.ResetToFreePlan();
            tenant.UpdatePlan(SubscriptionPlan.Basis);
            events.CollectEvent(new SubscriptionCancelled(subscription.Id, previousPlan, CancellationReason.CancelledByAdmin, 0, daysOnCurrentPlan, previousPriceAmount!.Value, -previousPriceAmount.Value, previousPriceCurrency!));
        }

        if (subscriptionSuspended)
        {
            subscription.ResetToFreePlan();
            tenant.UpdatePlan(SubscriptionPlan.Basis);
            tenant.Suspend(SuspensionReason.PaymentFailed, now);
            events.CollectEvent(new SubscriptionSuspended(subscription.Id, previousPlan, SuspensionReason.PaymentFailed, previousPriceAmount!.Value, -previousPriceAmount.Value, previousPriceCurrency!));
        }

        if (paymentFailed)
        {
            subscription.SetPaymentFailed(now);
            events.CollectEvent(new PaymentFailed(subscription.Id, subscription.Plan, subscription.CurrentPriceAmount!.Value, subscription.CurrentPriceCurrency!));
        }

        if (paymentRecovered)
        {
            var daysInPastDue = (int)(now - subscription.FirstPaymentFailedAt!.Value).TotalDays;
            subscription.ClearPaymentFailure();
            events.CollectEvent(new PaymentRecovered(subscription.Id, subscription.Plan, daysInPastDue, subscription.CurrentPriceAmount!.Value, subscription.CurrentPriceCurrency!));
        }

        if (paymentRefunded)
        {
            var refundedTransactions = subscription.PaymentTransactions.Where(t => t.Status == PaymentTransactionStatus.Refunded).ToArray();
            var refundCount = refundedTransactions.Length - previousRefundCount;
            var latestRefund = refundedTransactions[^1];
            var plan = stripeState is not null ? subscription.Plan : previousPlan;
            events.CollectEvent(new PaymentRefunded(subscription.Id, plan, refundCount, latestRefund.Amount, latestRefund.Currency));
        }

        var tenantChanged = stripeState is not null || subscriptionCreated || subscriptionExpired || subscriptionImmediatelyCancelled || subscriptionSuspended;
        if (tenantChanged)
        {
            tenantRepository.Update(tenant);
        }

        subscriptionRepository.Update(subscription);

        // Stripe snapshot built from the just-fetched Stripe state, paired with the pre-sync local snapshot
        // captured at the start of this method. When stripeState is null Stripe has no active subscription
        // for the customer, so the equivalent snapshot is the free plan with no price.
        var stripeSnapshot = stripeState is null
            ? new StripeSyncSnapshot(SubscriptionPlan.Basis, false, null, null)
            : new StripeSyncSnapshot(stripeState.Plan, stripeState.CancelAtPeriodEnd, stripeState.CurrentPriceAmount, stripeState.CurrentPriceCurrency);
        return new DriftSnapshots(localSnapshot, stripeSnapshot);
    }

    /// <summary>
    ///     Authoritative BillingEvent emission for the hot path. Inputs are strictly in-memory:
    ///     (1) the events.list response Stripe just returned (with payloads carried inline by
    ///     <see cref="StripeReplayEvent" />) and (2) the just-arrived webhook events whose payload was
    ///     posted to <c>AcknowledgeStripeWebhook</c> and is still loaded in memory as Pending. The
    ///     durable <c>stripe_events</c> archive is NEVER queried in this pass — its <c>Payload</c> column
    ///     is a cold backup only, read by the admin reconcile command. Stripe retains events for 30 days
    ///     (see https://docs.stripe.com/api/events); the background sweeper keeps the anchor inside the
    ///     window so the events.list view is always complete.
    ///     Idempotent on <c>billing_events.stripe_event_id</c>: rows whose source Stripe event id is
    ///     already recorded are skipped, so re-running this for every webhook (or via the back-office
    ///     reconcile action) is safe.
    /// </summary>
    private async Task EmitBillingEventsFromEventsListAsync(
        Subscription subscription,
        StripeEvent[] pendingEvents,
        StripeReplayEvent[] eventsListResults,
        DriftSnapshots driftSnapshots,
        CancellationToken cancellationToken
    )
    {
        if (subscription.StripeCustomerId is null) return;

        // Union the events.list view of the world with the just-arrived webhooks still Pending in this
        // transaction. Stripe's events.list typically reflects a new webhook within a few seconds, but the
        // hot path can't depend on that. Dedup by event id; the events.list payload wins when both sources
        // describe the same event (Stripe's serialization is the authoritative view).
        var unioned = new Dictionary<string, StripeReplayEvent>(eventsListResults.Length + pendingEvents.Length);
        foreach (var eventListItem in eventsListResults)
        {
            unioned[eventListItem.EventId] = eventListItem;
        }

        foreach (var pending in pendingEvents)
        {
            if (unioned.ContainsKey(pending.Id.Value)) continue;
            // pending.Payload is the webhook body posted to AcknowledgeStripeWebhook — carried in-memory
            // from the same request, not read from the cold durable archive.
            // Source the replay timestamp from Stripe's authoritative Event.Created (captured at ingestion
            // into StripeCreatedAt) so the replayer orders events and writes BillingEvent.OccurredAt at the
            // moment Stripe says the event occurred.
            unioned[pending.Id.Value] = new StripeReplayEvent(pending.Id.Value, pending.EventType, pending.StripeCreatedAt ?? pending.CreatedAt, pending.Payload ?? "", pending.ApiVersion);
        }

        if (unioned.Count == 0)
        {
            DetectDrift(subscription, driftSnapshots, 0, false, [], [], [], []);
            return;
        }

        var unsupportedVersions = new HashSet<string?>();
        var supportedEvents = new List<StripeReplayEvent>(unioned.Count);
        foreach (var stripeEvent in unioned.Values)
        {
            if (StripeEventPayloadResolverFactory.TryFor(stripeEvent.ApiVersion, out _))
            {
                supportedEvents.Add(stripeEvent);
                continue;
            }

            if (unsupportedVersions.Add(stripeEvent.ApiVersion))
            {
                logger.LogWarning(
                    "Stripe event {EventId} has unsupported api_version '{ApiVersion}'; replay skipped — add an IStripeEventPayloadResolver implementation",
                    stripeEvent.EventId, stripeEvent.ApiVersion ?? "null"
                );
            }
        }

        var stripeClient = stripeClientFactory.GetClient();
        var planByPriceId = await stripeClient.GetPlanByPriceIdAsync(cancellationToken);
        var priceCatalog = await stripeClient.GetPriceCatalogAsync(cancellationToken);
        var priceByPlan = priceCatalog.ToDictionary(p => p.Plan, p => p.UnitAmount);

        var existingStripeEventIds = await billingEventRepository.GetExistingStripeEventIdsUnfilteredAsync(subscription.Id, cancellationToken);
        var state = new StripeEventReplayer.ReplayState();

        // Several SyncStateFromStripe branches (subscriptionExpired, subscriptionImmediatelyCancelled,
        // subscriptionSuspended, IsCustomerDeleted) call Subscription.ResetToFreePlan which nulls
        // CurrentPriceCurrency BEFORE this emission runs, so the live subscription is no longer authoritative.
        // Prefer the just-fetched Stripe view, otherwise fall back to the pre-sync local snapshot — both
        // were captured before any mutation. The replayer still tries the per-event payload first.
        var currencyOverride = driftSnapshots.Stripe?.CurrentPriceCurrency ?? driftSnapshots.LocalBeforeSync.CurrentPriceCurrency;
        var replayedEvents = StripeEventReplayer.Replay(subscription, [.. supportedEvents], planByPriceId, priceByPlan, state, currencyOverride, logger);

        var appendedCount = 0;
        foreach (var billingEvent in replayedEvents)
        {
            if (existingStripeEventIds.Contains(billingEvent.StripeEventId)) continue;
            await billingEventRepository.AddAsync(billingEvent, cancellationToken);
            appendedCount++;

            // SubscribedSince is a denormalized cache of MIN(occurred_at) across SubscriptionCreated rows for
            // the tenant. AdvanceSubscribedSinceBackwardFromBillingEvent is monotonic-backward: a late-arriving
            // recovered event can rewind it earlier, but a new subscription started after a cancel (later
            // OccurredAt) cannot move it forward.
            if (billingEvent.EventType == BillingEventType.SubscriptionCreated)
            {
                subscription.AdvanceSubscribedSinceBackwardFromBillingEvent(billingEvent.OccurredAt);
            }
        }

        // Advance the events.list anchor to the most recent event we just consumed so the next sync only
        // pulls events Stripe produced after this point. Pending-source events whose Created is older than
        // an already-applied anchor cannot rewind it (AdvanceLastSyncedStripeEventCreatedAt is monotonic).
        if (supportedEvents.Count > 0)
        {
            var latestEventCreated = supportedEvents.Max(e => e.CreatedAt);
            subscription.AdvanceLastSyncedStripeEventCreatedAt(latestEventCreated);
        }

        // Out-of-order recovery (e.g. a customer.subscription.created arriving after a later
        // customer.subscription.deleted was already classified and persisted) leaves the persisted row's
        // denormalized fields wrong for the now-correct state-machine ordering. The append-only invariant
        // forbids mutating the persisted row, so surface the wrongness via drift instead so an operator
        // can investigate.
        var persistedRows = await billingEventRepository.GetBySubscriptionIdUnfilteredAsync(subscription.Id, cancellationToken);
        var persistedByStripeId = persistedRows.ToDictionary(r => r.StripeEventId);
        var staleBillingEvents = new List<BillingEvent>();
        foreach (var replayed in replayedEvents)
        {
            if (!persistedByStripeId.TryGetValue(replayed.StripeEventId, out var persisted)) continue;
            if (persisted.CommittedMrr != replayed.CommittedMrr
                || persisted.AmountDelta != replayed.AmountDelta
                || persisted.PreviousAmount != replayed.PreviousAmount
                || persisted.NewAmount != replayed.NewAmount)
            {
                staleBillingEvents.Add(replayed);
            }
        }

        var totalBillingEvents = existingStripeEventIds.Count + appendedCount;
        var eventTypesPresent = unioned.Values.Select(e => e.EventType).ToHashSet();
        var billingEventTypesPresent = persistedRows.Select(r => r.EventType)
            .Concat(replayedEvents.Select(r => r.EventType))
            .ToHashSet();
        DetectDrift(subscription, driftSnapshots, totalBillingEvents, state.HasUnclassifiedEvent, unsupportedVersions, eventTypesPresent, billingEventTypesPresent, staleBillingEvents);
    }

    private void DetectDrift(
        Subscription subscription,
        DriftSnapshots driftSnapshots,
        int billingEventCount,
        bool hasUnclassifiedEvent,
        HashSet<string?> unsupportedApiVersions,
        HashSet<string> eventTypesPresent,
        HashSet<BillingEventType> billingEventTypesPresent,
        IReadOnlyList<BillingEvent> staleBillingEvents
    )
    {
        var now = timeProvider.GetUtcNow();
        try
        {
            // The Stripe snapshot is null when the customer fetch failed earlier in the sync, so fall back
            // to the local pre-sync view (no SubscriptionStateMismatch can be detected without a Stripe view,
            // but the other coverage checks still run).
            var stripeSnapshot = driftSnapshots.Stripe ?? driftSnapshots.LocalBeforeSync;
            var discrepancies = BillingDriftDetector.Detect(driftSnapshots.LocalBeforeSync, stripeSnapshot, subscription.PaymentTransactions.Length, billingEventCount);
            if (hasUnclassifiedEvent)
            {
                discrepancies = discrepancies.Add(new DriftDiscrepancy(
                        DriftDiscrepancyKind.UnclassifiedStripeEvent,
                        "Stripe sent a subscription update combining multiple changes that don't decompose into a single domain transition. Investigate in Stripe Dashboard.",
                        DriftSeverity.Warning
                    )
                );
            }

            foreach (var version in unsupportedApiVersions)
            {
                discrepancies = discrepancies.Add(new DriftDiscrepancy(
                        DriftDiscrepancyKind.UnsupportedStripeApiVersion,
                        $"Stripe sent an event using api_version '{version ?? "null"}' for which no IStripeEventPayloadResolver is registered. The event is preserved in stripe_events but not replayed into billing_events. Add a resolver and re-sync.",
                        DriftSeverity.Critical,
                        ActualValue: version
                    )
                );
            }

            foreach (var staleRow in staleBillingEvents)
            {
                discrepancies = discrepancies.Add(new DriftDiscrepancy(
                        DriftDiscrepancyKind.BillingEventDenormalizationStale,
                        $"Persisted billing_event '{staleRow.StripeEventId}' has stale denormalized fields. Replay produced different CommittedMrr/AmountDelta/PreviousAmount/NewAmount values; this indicates an out-of-order event recovery. The persisted row is left untouched per the append-only invariant.",
                        DriftSeverity.Warning,
                        staleRow.EventType,
                        OccurredAt: staleRow.OccurredAt
                    )
                );
            }

            var coverageDiscrepancies = CheckResourceCoverage(subscription, now, eventTypesPresent, billingEventTypesPresent);
            discrepancies = discrepancies.AddRange(coverageDiscrepancies);

            subscription.SetDriftStatus(discrepancies, now);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Drift detection threw while syncing Stripe customer '{StripeCustomerId}', existing drift status preserved", subscription.StripeCustomerId);
        }
    }

    /// <summary>
    ///     Per-resource audit: walks every Stripe-tracked resource on the subscription and verifies
    ///     a corresponding event exists in the in-memory union of events.list output and just-arrived
    ///     webhook events. Each missing event becomes a drift discrepancy. The recovery countdown is
    ///     driven by the resource's known timestamp: if the resource timestamp is within Stripe's
    ///     30-day events.list window the discrepancy is
    ///     <see cref="DriftDiscrepancyKind.MissingHistoricalEvent" /> (auto-recoverable on next
    ///     reconciliation pass); past the window it escalates to
    ///     <see cref="DriftDiscrepancyKind.MissingHistoricalEventUnrecoverable" /> (P1: data is
    ///     permanently lost from Stripe and must be investigated as a reconciliation bug).
    ///     TODO: if a coverage kind proves too noisy in production, expose per-kind suppression via
    ///     IConfiguration (e.g. BillingDrift:DisabledCoverageKinds:0=PaymentMethodAttached) so operators
    ///     can silence it without a deploy.
    /// </summary>
    private static DriftDiscrepancy[] CheckResourceCoverage(Subscription subscription, DateTimeOffset now, HashSet<string> eventTypesPresent, HashSet<BillingEventType> billingEventTypesPresent)
    {
        var discrepancies = new List<DriftDiscrepancy>();
        if (subscription.StripeCustomerId is null) return [.. discrepancies];

        // Source the SubscriptionCreated coverage check from the BillingEvent log, not the denormalized
        // SubscribedSince column. The log is authoritative: any SubscriptionCreated row means a paid run
        // happened, so the corresponding customer.subscription.created stripe_events row must exist too.
        if (billingEventTypesPresent.Contains(BillingEventType.SubscriptionCreated) && !eventTypesPresent.Contains("customer.subscription.created"))
        {
            var subscribedSince = subscription.SubscribedSince ?? subscription.CreatedAt;
            discrepancies.Add(BuildCoverageDiscrepancy(
                    "customer.subscription.created", subscribedSince, now,
                    "Subscription has a SubscriptionCreated billing_events row but no customer.subscription.created stripe_events row is recorded.",
                    BillingEventType.SubscriptionCreated
                )
            );
        }

        var succeededTransactions = subscription.PaymentTransactions.Where(t => t.Status == PaymentTransactionStatus.Succeeded).ToArray();
        if (succeededTransactions.Length > 0 && !eventTypesPresent.Contains("invoice.payment_succeeded"))
        {
            var earliest = succeededTransactions.Min(t => t.Date);
            discrepancies.Add(BuildCoverageDiscrepancy(
                    "invoice.payment_succeeded", earliest, now,
                    $"Subscription has {succeededTransactions.Length} succeeded payments but no invoice.payment_succeeded event is recorded.",
                    BillingEventType.SubscriptionRenewed
                )
            );
        }

        var refundedTransactions = subscription.PaymentTransactions.Where(t => t.Status == PaymentTransactionStatus.Refunded).ToArray();
        if (refundedTransactions.Length > 0 && !eventTypesPresent.Contains("charge.refunded"))
        {
            var earliestRefund = refundedTransactions.Min(t => t.RefundedAt ?? t.Date);
            discrepancies.Add(BuildCoverageDiscrepancy(
                    "charge.refunded", earliestRefund, now,
                    $"Subscription has {refundedTransactions.Length} refunded payments but no charge.refunded event is recorded.",
                    BillingEventType.PaymentRefunded
                )
            );
        }

        if (subscription.ScheduledPlan is not null && !eventTypesPresent.Contains("subscription_schedule.updated"))
        {
            var scheduledAt = subscription.ModifiedAt ?? subscription.CreatedAt;
            discrepancies.Add(BuildCoverageDiscrepancy(
                    "subscription_schedule.updated", scheduledAt, now,
                    $"Subscription has a scheduled plan ({subscription.ScheduledPlan}) but no subscription_schedule.updated event is recorded.",
                    BillingEventType.SubscriptionDowngradeScheduled
                )
            );
        }

        if (subscription.PaymentMethod is not null && !eventTypesPresent.Contains("payment_method.attached"))
        {
            var attachedAt = subscription.SubscribedSince ?? subscription.CreatedAt;
            discrepancies.Add(BuildCoverageDiscrepancy(
                    "payment_method.attached", attachedAt, now,
                    "Subscription has a payment method but no payment_method.attached event is recorded.",
                    BillingEventType.PaymentMethodUpdated
                )
            );
        }

        return [.. discrepancies];
    }

    private static DriftDiscrepancy BuildCoverageDiscrepancy(
        string expectedEventType,
        DateTimeOffset eventOccurredAt,
        DateTimeOffset now,
        string description,
        BillingEventType billingEventType
    )
    {
        var stripeRetentionWindow = TimeSpan.FromDays(30);
        var withinWindow = now - eventOccurredAt < stripeRetentionWindow;
        var kind = withinWindow ? DriftDiscrepancyKind.MissingHistoricalEvent : DriftDiscrepancyKind.MissingHistoricalEventUnrecoverable;
        var severity = withinWindow ? DriftSeverity.Warning : DriftSeverity.Critical;
        return new DriftDiscrepancy(
            kind,
            $"{description} Expected event type: {expectedEventType}.",
            severity,
            billingEventType,
            OccurredAt: eventOccurredAt
        );
    }

    /// <summary>
    ///     Pulls Stripe's events.list for the customer (anchored on
    ///     <see cref="Subscription.LastSyncedStripeEventCreatedAt" />) and inserts any event ids Stripe
    ///     knows about but the local archive doesn't into <c>stripe_events</c> as recovered rows. The
    ///     archive is a cold backup only; the events.list response itself is what drives BillingEvent
    ///     emission in <see cref="EmitBillingEventsFromEventsListAsync" />, so the local payload column
    ///     is never read in the hot path. Returns the events.list response so the emitter can consume
    ///     it directly.
    /// </summary>
    private async Task<StripeReplayEvent[]> PullEventsListAndArchiveRecoveredAsync(Subscription subscription, StripeCustomerId stripeCustomerId, CancellationToken cancellationToken)
    {
        var stripeClient = stripeClientFactory.GetClient();
        var stripeEvents = await stripeClient.GetEventsForCustomerAsync(stripeCustomerId, subscription.LastSyncedStripeEventCreatedAt, cancellationToken);
        if (stripeEvents.Length == 0) return [];

        var existingIds = await stripeEventRepository.GetExistingEventIdsByStripeCustomerIdAsync(stripeCustomerId, cancellationToken);
        var now = timeProvider.GetUtcNow();

        foreach (var stripeEvent in stripeEvents)
        {
            if (existingIds.Contains(stripeEvent.EventId)) continue;

            var payloadHash = StripeEventPayloadHasher.Hash(stripeEvent.Payload);
            var recoveredEvent = StripeEvent.CreateRecovered(
                stripeEvent.EventId,
                stripeEvent.EventType,
                stripeCustomerId,
                stripeEvent.Payload,
                stripeEvent.ApiVersion,
                payloadHash,
                now,
                "events_list",
                stripeEvent.CreatedAt
            );
            await stripeEventRepository.AddAsync(recoveredEvent, cancellationToken);

            events.CollectEvent(new WebhookDeliveryRecovered(stripeEvent.EventId, stripeEvent.EventType, "events_list"));
            logger.LogWarning(
                "Recovered Stripe event {EventId} ({EventType}) for customer '{StripeCustomerId}' from events.list — webhook delivery was missed",
                stripeEvent.EventId, stripeEvent.EventType, stripeCustomerId
            );
        }

        return stripeEvents;
    }

    private void MarkAllEventsAsProcessed(StripeEvent[] pendingEvents, Subscription subscription)
    {
        var now = timeProvider.GetUtcNow();

        foreach (var pendingEvent in pendingEvents)
        {
            pendingEvent.MarkProcessed(now, subscription.TenantId, subscription.StripeSubscriptionId);
            stripeEventRepository.Update(pendingEvent);
        }
    }

    private void SendTelemetryEvents(Tenant tenant, Subscription subscription)
    {
        TenantScopedTelemetryContext.Set(tenant.Id, subscription.Plan.ToString());

        while (events.HasEvents)
        {
            var telemetryEvent = events.Dequeue();
            telemetryClient.TrackEvent(telemetryEvent.GetType().Name, telemetryEvent.Properties);
            logger.LogInformation("Telemetry: {EventName} {EventProperties}", telemetryEvent.GetType().Name, string.Join(", ", telemetryEvent.Properties.Select(p => $"{p.Key}={p.Value}")));
        }
    }

    private sealed record DriftSnapshots(StripeSyncSnapshot LocalBeforeSync, StripeSyncSnapshot? Stripe);
}
