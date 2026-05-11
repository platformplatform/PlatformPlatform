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
    DateTimeOffset ReconciledAt
);

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
    ProcessPendingStripeEvents processPendingStripeEvents,
    StripeClientFactory stripeClientFactory,
    TimeProvider timeProvider,
    ITelemetryEventsCollector events
) : IRequestHandler<ReconcileTenantWithStripeCommand, Result<ReconcileTenantWithStripeResponse>>
{
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
        // without grouping by currency, so any non-DKK row corrupts the totals. Reject non-DKK at the
        // boundary before reconcile mutates persistence. The DB CHECK constraint is the structural backstop.
        var stripeState = await stripeClientFactory.GetClient().SyncSubscriptionStateAsync(subscription.StripeCustomerId, cancellationToken);
        if (stripeState?.CurrentPriceCurrency is { } observedCurrency && observedCurrency != "DKK")
        {
            events.CollectEvent(new StripeNonDkkSubscriptionRejected(subscription.Id, observedCurrency));
            return Result<ReconcileTenantWithStripeResponse>.BadRequest($"Subscription currency '{observedCurrency}' is not supported. Only DKK is currently supported.", true);
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

        var response = new ReconcileTenantWithStripeResponse(
            billingEventsAppended,
            hasDriftDetected,
            driftDiscrepancyCount,
            timeProvider.GetUtcNow()
        );

        events.CollectEvent(new TenantReconciledWithStripe(subscription.Id, billingEventsAppended));

        return response;
    }
}
