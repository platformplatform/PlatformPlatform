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
public sealed record ReconcileTenantWithStripeCommand : ICommand, IRequest<Result<ReconcileTenantWithStripeResponse>>
{
    [JsonIgnore] // Removes from API contract
    public TenantId TenantId { get; init; } = null!;
}

[PublicAPI]
public sealed record ReconcileTenantWithStripeResponse(
    int BillingEventsAppended,
    bool HasDriftDetected,
    int DriftDiscrepancyCount,
    DateTimeOffset ReconciledAt,
    ArchivedEventsAwaitingConfirmation? ArchivedEventsAwaitingConfirmation
);

/// <summary>
///     Set on <see cref="ReconcileTenantWithStripeResponse" /> when the local stripe_events archive contains
///     events older than Stripe's 30-day events.list retention window that have no matching billing_events
///     row yet. The reconcile flow never auto-replays archive data — surfacing this block tells the
///     back-office admin to confirm before <c>ReplayArchivedTenantStripeEventsCommand</c> projects the
///     cold-backup payloads into the BillingEvent ledger.
/// </summary>
[PublicAPI]
public sealed record ArchivedEventsAwaitingConfirmation(int Count, DateTimeOffset OldestOccurredAt, DateTimeOffset NewestOccurredAt);

/// <summary>
///     Reconcile is the admin recovery path for a tenant's BillingEvent ledger. It runs the same
///     events.list-driven sync as the webhook hot path, then additionally falls back to the local
///     <c>stripe_events.payload</c> cold backup for any event older than Stripe's 30-day events.list
///     retention window. The hot path never reads <c>stripe_events.payload</c>; reconcile is the
///     only code path that does, and it is expected to be rare.
/// </summary>
public sealed class ReconcileTenantWithStripeHandler(
    ITenantRepository tenantRepository,
    ISubscriptionRepository subscriptionRepository,
    IBillingEventRepository billingEventRepository,
    IStripeEventRepository stripeEventRepository,
    ProcessPendingStripeEvents processPendingStripeEvents,
    StripeClientFactory stripeClientFactory,
    IPlatformCurrencyProvider platformCurrencyProvider,
    TimeProvider timeProvider,
    ITelemetryEventsCollector events
) : IRequestHandler<ReconcileTenantWithStripeCommand, Result<ReconcileTenantWithStripeResponse>>
{
    /// <summary>
    ///     Stripe retains events for 30 days via its events.list API (see https://docs.stripe.com/api/events).
    ///     Anything older must be replayed from the local stripe_events archive — but only after the operator
    ///     confirms, because the cold backup carries the payload Stripe served at ingestion time and replay
    ///     may write approximate data when the catalog has rolled forward since.
    /// </summary>
    private static readonly TimeSpan StripeEventsListRetentionWindow = TimeSpan.FromDays(30);

    public async Task<Result<ReconcileTenantWithStripeResponse>> Handle(ReconcileTenantWithStripeCommand command, CancellationToken cancellationToken)
    {
        if (stripeClientFactory.GetClient() is UnconfiguredStripeClient)
        {
            return Result<ReconcileTenantWithStripeResponse>.BadRequest("Stripe is not configured in this environment, reconcile is unavailable.");
        }

        var tenant = await tenantRepository.GetByIdUnfilteredAsync(command.TenantId, cancellationToken);
        if (tenant is null) return Result<ReconcileTenantWithStripeResponse>.NotFound($"Tenant with id '{command.TenantId}' not found.");

        var subscription = await subscriptionRepository.GetByTenantIdUnfilteredAsync(command.TenantId, cancellationToken);
        if (subscription is null) return Result<ReconcileTenantWithStripeResponse>.NotFound($"Subscription for tenant '{command.TenantId}' not found.");

        if (subscription.StripeCustomerId is null)
        {
            return Result<ReconcileTenantWithStripeResponse>.BadRequest("Tenant has no Stripe customer to reconcile with.");
        }

        // Single-currency invariant: the dashboard MRR handlers sum across all subscriptions / billing events
        // without grouping by currency, so any row that does not use the platform currency corrupts the totals.
        // Reject mismatched currencies at the boundary before reconcile mutates persistence. The DB CHECK
        // constraint enforces the format invariant as a structural backstop.
        var stripeState = await stripeClientFactory.GetClient().SyncSubscriptionStateAsync(subscription.StripeCustomerId, cancellationToken);
        var platformCurrency = platformCurrencyProvider.Currency;
        if (stripeState?.CurrentPriceCurrency is { } observedCurrency && platformCurrency is not null && observedCurrency != platformCurrency)
        {
            events.CollectEvent(new StripeSubscriptionCurrencyMismatchRejected(subscription.StripeSubscriptionId?.Value ?? subscription.Id.Value, observedCurrency, platformCurrency));
            return Result<ReconcileTenantWithStripeResponse>.BadRequest($"Subscription currency '{observedCurrency}' does not match the platform currency '{platformCurrency}'.", true);
        }

        var beforeEvents = await billingEventRepository.GetBySubscriptionIdUnfilteredAsync(subscription.Id, cancellationToken);

        await processPendingStripeEvents.ExecuteAsync(subscription.StripeCustomerId, true, SyncMode.Apply, cancellationToken);

        var afterEvents = await billingEventRepository.GetBySubscriptionIdUnfilteredAsync(subscription.Id, cancellationToken);
        var billingEventsAppended = afterEvents.Length - beforeEvents.Length;

        // Reload the subscription so drift fields reflect the just-completed reconcile. ExecuteAsync runs in its own
        // transaction and the previously-fetched aggregate is detached, so we read the freshly persisted state.
        var refreshedSubscription = await subscriptionRepository.GetByTenantIdUnfilteredAsync(command.TenantId, cancellationToken);
        var hasDriftDetected = refreshedSubscription?.HasDriftDetected ?? false;
        var driftDiscrepancyCount = refreshedSubscription?.DriftDiscrepancies.Length ?? 0;

        // Bootstrap and stale-anchor recovery: the events.list anchor only covers the last 30 days, so
        // archive payloads older than that window can only land in billing_events via explicit replay.
        // Surface the count and date range here; replay is gated behind the admin confirmation dialog and
        // ReplayArchivedTenantStripeEventsCommand — never auto-applied from this handler.
        var now = timeProvider.GetUtcNow();
        var archiveCutoff = now - StripeEventsListRetentionWindow;
        var archivedAwaitingReplay = await stripeEventRepository.GetArchivedEventsOlderThanAsync(subscription.StripeCustomerId, archiveCutoff, cancellationToken);
        // GetArchivedEventsOlderThanAsync filters on `StripeCreatedAt < cutoff`, which excludes NULLs by
        // SQL/LINQ semantics, so every row returned is guaranteed to have a non-null StripeCreatedAt.
        var archivedEventsAwaitingConfirmation = archivedAwaitingReplay.Length > 0
            ? new ArchivedEventsAwaitingConfirmation(
                archivedAwaitingReplay.Length,
                archivedAwaitingReplay.Min(e => e.StripeCreatedAt!.Value),
                archivedAwaitingReplay.Max(e => e.StripeCreatedAt!.Value)
            )
            : null;

        var response = new ReconcileTenantWithStripeResponse(
            billingEventsAppended,
            hasDriftDetected,
            driftDiscrepancyCount,
            now,
            archivedEventsAwaitingConfirmation
        );

        events.CollectEvent(new TenantReconciledWithStripe(subscription.Id, billingEventsAppended));

        return response;
    }
}
