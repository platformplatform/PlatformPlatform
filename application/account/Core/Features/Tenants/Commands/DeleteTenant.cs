using Account.Features.FeatureFlags.Domain;
using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;
using SharedKernel.Telemetry;

namespace Account.Features.Tenants.Commands;

[PublicAPI]
public sealed record DeleteTenantCommand(TenantId Id) : ICommand, IRequest<Result>;

public sealed class DeleteTenantHandler(
    ITenantRepository tenantRepository,
    ISubscriptionRepository subscriptionRepository,
    IFeatureFlagRepository featureFlagRepository,
    ITelemetryEventsCollector events
) : IRequestHandler<DeleteTenantCommand, Result>
{
    public async Task<Result> Handle(DeleteTenantCommand command, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdAsync(command.Id, cancellationToken);
        if (tenant is null) return Result.NotFound($"Tenant with id '{command.Id}' not found.");

        var subscription = await subscriptionRepository.GetByTenantIdUnfilteredAsync(command.Id, cancellationToken);
        if (subscription?.HasActiveStripeSubscription() == true)
        {
            return Result.BadRequest("Cannot delete a tenant with an active subscription.");
        }

        // Cascade-delete tenant-scoped feature flag rows so soft-deleted tenants don't leak orphaned
        // override / plan-source rows in feature_flags. The reconciler does not sweep these rows, and
        // re-creating a tenant with the same Stripe customer ID would otherwise inherit stale state.
        var tenantFlagRows = await featureFlagRepository.GetRowsByTenantAsync(command.Id, cancellationToken);
        foreach (var row in tenantFlagRows)
        {
            featureFlagRepository.Remove(row);
        }

        tenantRepository.Remove(tenant);

        events.CollectEvent(new TenantDeleted(tenant.Id, tenant.State, tenantFlagRows.Length));

        return Result.Success();
    }
}
