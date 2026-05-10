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
public sealed record SyncTenantWithStripeCommand : ICommand, IRequest<Result<SyncTenantWithStripeResponse>>
{
    [JsonIgnore] // Removes from API contract
    public TenantId TenantId { get; init; } = null!;
}

[PublicAPI]
public sealed record SyncTenantWithStripeResponse(
    int BillingEventsAppended,
    bool HasDriftDetected,
    int DriftDiscrepancyCount,
    DateTimeOffset SyncedAt
);

public sealed class SyncTenantWithStripeHandler(
    ITenantRepository tenantRepository,
    ISubscriptionRepository subscriptionRepository,
    IBillingEventRepository billingEventRepository,
    ProcessPendingStripeEvents processPendingStripeEvents,
    StripeClientFactory stripeClientFactory,
    TimeProvider timeProvider,
    ITelemetryEventsCollector events
) : IRequestHandler<SyncTenantWithStripeCommand, Result<SyncTenantWithStripeResponse>>
{
    public async Task<Result<SyncTenantWithStripeResponse>> Handle(SyncTenantWithStripeCommand command, CancellationToken cancellationToken)
    {
        if (stripeClientFactory.GetClient() is UnconfiguredStripeClient)
        {
            return Result<SyncTenantWithStripeResponse>.BadRequest("Stripe is not configured in this environment, sync is unavailable.");
        }

        var tenant = await tenantRepository.GetByIdUnfilteredAsync(command.TenantId, cancellationToken);
        if (tenant is null) return Result<SyncTenantWithStripeResponse>.NotFound($"Tenant with id '{command.TenantId}' not found.");

        var subscription = await subscriptionRepository.GetByTenantIdUnfilteredAsync(command.TenantId, cancellationToken);
        if (subscription is null) return Result<SyncTenantWithStripeResponse>.NotFound($"Subscription for tenant '{command.TenantId}' not found.");

        if (subscription.StripeCustomerId is null)
        {
            return Result<SyncTenantWithStripeResponse>.BadRequest("Tenant has no Stripe customer to sync with.");
        }

        // Single-currency invariant: the dashboard MRR handlers sum across all subscriptions / billing events
        // without grouping by currency, so any non-DKK row corrupts the totals. Reject non-DKK at the
        // boundary before the sync mutates persistence. The DB CHECK constraint is the structural backstop.
        var stripeState = await stripeClientFactory.GetClient().SyncSubscriptionStateAsync(subscription.StripeCustomerId, cancellationToken);
        if (stripeState?.CurrentPriceCurrency is { } observedCurrency && observedCurrency != "DKK")
        {
            events.CollectEvent(new StripeNonDkkSubscriptionRejected(subscription.Id, observedCurrency));
            return Result<SyncTenantWithStripeResponse>.BadRequest($"Subscription currency '{observedCurrency}' is not supported. Only DKK is currently supported.", true);
        }

        var beforeEvents = await billingEventRepository.GetBySubscriptionIdUnfilteredAsync(subscription.Id, cancellationToken);

        await processPendingStripeEvents.ExecuteAsync(subscription.StripeCustomerId, true, cancellationToken);

        var afterEvents = await billingEventRepository.GetBySubscriptionIdUnfilteredAsync(subscription.Id, cancellationToken);
        var billingEventsAppended = afterEvents.Length - beforeEvents.Length;

        // Reload the subscription so drift fields reflect the just-completed sync. ExecuteAsync runs in its own
        // transaction and the previously-fetched aggregate is detached, so we read the freshly persisted state.
        var refreshedSubscription = await subscriptionRepository.GetByTenantIdUnfilteredAsync(command.TenantId, cancellationToken);
        var hasDriftDetected = refreshedSubscription?.HasDriftDetected ?? false;
        var driftDiscrepancyCount = refreshedSubscription?.DriftDiscrepancies.Length ?? 0;

        var response = new SyncTenantWithStripeResponse(
            billingEventsAppended,
            hasDriftDetected,
            driftDiscrepancyCount,
            timeProvider.GetUtcNow()
        );

        events.CollectEvent(new TenantSyncedWithStripe(subscription.Id, billingEventsAppended));

        return response;
    }
}
