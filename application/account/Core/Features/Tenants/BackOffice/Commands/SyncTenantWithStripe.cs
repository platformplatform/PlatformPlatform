using Account.Features.Subscriptions.Domain;
using Account.Features.Subscriptions.Shared;
using Account.Features.Tenants.Domain;
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
    TimeProvider timeProvider,
    ITelemetryEventsCollector events
) : IRequestHandler<SyncTenantWithStripeCommand, Result<SyncTenantWithStripeResponse>>
{
    public async Task<Result<SyncTenantWithStripeResponse>> Handle(SyncTenantWithStripeCommand command, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdUnfilteredAsync(command.TenantId, cancellationToken);
        if (tenant is null) return Result<SyncTenantWithStripeResponse>.NotFound($"Tenant with id '{command.TenantId}' not found.");

        var subscription = await subscriptionRepository.GetByTenantIdUnfilteredAsync(command.TenantId, cancellationToken);
        if (subscription is null) return Result<SyncTenantWithStripeResponse>.NotFound($"Subscription for tenant '{command.TenantId}' not found.");

        if (subscription.StripeCustomerId is null)
        {
            return Result<SyncTenantWithStripeResponse>.BadRequest("Tenant has no Stripe customer to sync with.");
        }

        var beforeEvents = await billingEventRepository.GetBySubscriptionIdUnfilteredAsync(subscription.Id, cancellationToken);

        await processPendingStripeEvents.ExecuteAsync(subscription.StripeCustomerId, true, cancellationToken);

        var afterEvents = await billingEventRepository.GetBySubscriptionIdUnfilteredAsync(subscription.Id, cancellationToken);
        var billingEventsAppended = afterEvents.Length - beforeEvents.Length;

        // Drift fields are stubbed until PP-1204 lands. The shape is wired now so the frontend dialog can render
        // forward-compatible content; once drift detection is in place these values reflect the actual state.
        var response = new SyncTenantWithStripeResponse(
            billingEventsAppended,
            false,
            0,
            timeProvider.GetUtcNow()
        );

        events.CollectEvent(new TenantSyncedWithStripe(subscription.Id, billingEventsAppended));

        return response;
    }
}
