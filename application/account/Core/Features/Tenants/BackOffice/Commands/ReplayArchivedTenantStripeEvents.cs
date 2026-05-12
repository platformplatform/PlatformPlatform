using Account.Features.Subscriptions.Domain;
using Account.Features.Subscriptions.Shared;
using Account.Features.Tenants.Domain;
using Account.Integrations.Stripe;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;
using SharedKernel.Telemetry;

namespace Account.Features.Tenants.BackOffice.Commands;

[PublicAPI]
public sealed record ReplayArchivedTenantStripeEventsCommand : ICommand, IRequest<Result<ReplayArchivedTenantStripeEventsResponse>>
{
    [JsonIgnore] // Removes from API contract
    public TenantId TenantId { get; init; } = null!;
}

[PublicAPI]
public sealed record ReplayArchivedTenantStripeEventsResponse(int BillingEventsAppended, DateTimeOffset ReplayedAt);

/// <summary>
///     Replays archived stripe_events rows that Stripe's events.list no longer returns for the customer
///     into the BillingEvent ledger. This is the cold-backup recovery path explicitly opted into by an
///     admin after <see cref="ReconcileTenantWithStripeCommand" /> surfaced
///     <see cref="ArchivedEventsAwaitingConfirmation" />. The handler is the only writer that reads
///     <c>stripe_events.payload</c> from outside the same-transaction hot path — every other code path
///     drives BillingEvent emission from Stripe's events.list view of the world.
///     The boundary between "Stripe still has it" and "archive is the only source" comes from asking
///     Stripe directly: the handler calls events.list, collects the returned event ids, and replays
///     every archived row whose id is NOT in that set. ID comparison is exact and avoids the
///     off-by-one fragility of any date-based cutoff (Stripe documents "approximately 30 days" and
///     same-second sibling events would split or duplicate under a date boundary).
/// </summary>
public sealed class ReplayArchivedTenantStripeEventsHandler(
    ITenantRepository tenantRepository,
    ISubscriptionRepository subscriptionRepository,
    IBillingEventRepository billingEventRepository,
    IStripeEventRepository stripeEventRepository,
    StripeClientFactory stripeClientFactory,
    TimeProvider timeProvider,
    ITelemetryEventsCollector events,
    ILogger<ReplayArchivedTenantStripeEventsHandler> logger
) : IRequestHandler<ReplayArchivedTenantStripeEventsCommand, Result<ReplayArchivedTenantStripeEventsResponse>>
{
    public async Task<Result<ReplayArchivedTenantStripeEventsResponse>> Handle(ReplayArchivedTenantStripeEventsCommand command, CancellationToken cancellationToken)
    {
        if (stripeClientFactory.GetClient() is UnconfiguredStripeClient)
        {
            return Result<ReplayArchivedTenantStripeEventsResponse>.BadRequest("Stripe is not configured in this environment, archive replay is unavailable.");
        }

        var tenant = await tenantRepository.GetByIdUnfilteredAsync(command.TenantId, cancellationToken);
        if (tenant is null) return Result<ReplayArchivedTenantStripeEventsResponse>.NotFound($"Tenant with id '{command.TenantId}' not found.");

        var subscription = await subscriptionRepository.GetByTenantIdUnfilteredAsync(command.TenantId, cancellationToken);
        if (subscription is null) return Result<ReplayArchivedTenantStripeEventsResponse>.NotFound($"Subscription for tenant '{command.TenantId}' not found.");

        if (subscription.StripeCustomerId is null)
        {
            return Result<ReplayArchivedTenantStripeEventsResponse>.BadRequest("Tenant has no Stripe customer to replay archived events for.");
        }

        // Ask Stripe what events it still serves for this customer; anything in the archive whose id is NOT
        // in that set is by definition outside the events.list window and is a candidate for replay. When
        // Stripe returns empty (customer dormant long enough that everything aged out), every archived row
        // qualifies — the archive is then our only source. Using ids avoids the same-second-sibling-event
        // ambiguity that any date cutoff has.
        var stripeClient = stripeClientFactory.GetClient();
        var stripeEventsList = await stripeClient.GetEventsForCustomerAsync(subscription.StripeCustomerId, null, cancellationToken);
        var knownStripeEventIds = stripeEventsList.Events.Select(e => e.EventId).ToHashSet();
        var archivedEvents = await stripeEventRepository.GetArchivedEventsExcludingAsync(subscription.StripeCustomerId, knownStripeEventIds, cancellationToken);
        if (archivedEvents.Length == 0)
        {
            return Result<ReplayArchivedTenantStripeEventsResponse>.BadRequest("No archived events outside Stripe's current events.list window are awaiting replay.");
        }

        var now = timeProvider.GetUtcNow();
        var planByPriceId = await stripeClient.GetPlanByPriceIdAsync(cancellationToken);
        var priceCatalog = await stripeClient.GetPriceCatalogAsync(cancellationToken);
        var priceByPlan = priceCatalog.ToDictionary(p => p.Plan, p => p.UnitAmount);

        // Seed the running state from the latest persisted BillingEvent so the archive batch carries forward
        // history rather than restarting at zero — without this seed the first SubscriptionCancelled in the
        // archive would emit committedMrr=0 and silently rewrite MRR. The replayer otherwise has no view of
        // the pre-archive state because the events feeding it predate every billing_events row.
        var persistedRows = await billingEventRepository.GetBySubscriptionIdUnfilteredAsync(subscription.Id, cancellationToken);
        var latestPersistedBillingEvent = persistedRows
            .OrderByDescending(r => r.OccurredAt)
            .ThenByDescending(r => r.Id.Value)
            .FirstOrDefault();
        var state = StripeEventReplayer.SeedReplayStateFromHistory(latestPersistedBillingEvent, subscription);

        // GetArchivedEventsExcludingAsync excludes rows with NULL StripeCreatedAt, so every row returned is
        // guaranteed to have a non-null timestamp. A null ApiVersion (Stripe omitted the field on a legacy
        // row) is passed as empty so the unsupported-version code path surfaces it as drift instead of
        // matching a real resolver.
        var replayInputs = archivedEvents
            .Select(e => new StripeReplayEvent(e.Id.Value, e.EventType, e.StripeCreatedAt!.Value, e.Payload ?? "", e.ApiVersion ?? ""))
            .ToArray();

        var existingStripeEventIds = await billingEventRepository.GetExistingStripeEventIdsUnfilteredAsync(subscription.Id, cancellationToken);
        var currencyOverride = subscription.CurrentPriceCurrency;
        var replayedEvents = StripeEventReplayer.Replay(subscription, replayInputs, planByPriceId, priceByPlan, state, currencyOverride, logger);

        var appendedCount = 0;
        foreach (var billingEvent in replayedEvents)
        {
            if (existingStripeEventIds.Contains(billingEvent.StripeEventId)) continue;
            await billingEventRepository.AddAsync(billingEvent, cancellationToken);

            if (billingEvent.EventType == BillingEventType.SubscriptionCreated)
            {
                subscription.AdvanceSubscribedSinceBackwardFromBillingEvent(billingEvent.OccurredAt);
            }

            appendedCount++;
        }

        if (appendedCount > 0)
        {
            subscriptionRepository.Update(subscription);
        }

        events.CollectEvent(new TenantStripeArchiveReplayed(appendedCount));

        return new ReplayArchivedTenantStripeEventsResponse(appendedCount, now);
    }
}
