using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;
using SharedKernel.Telemetry;

namespace Account.Features.Tenants.Commands;

[PublicAPI]
public sealed record DeleteTenantCommand(TenantId Id) : ICommand, IRequest<Result>;

public sealed class DeleteTenantHandler(ITenantRepository tenantRepository, ISubscriptionRepository subscriptionRepository, ITelemetryEventsCollector events)
    : IRequestHandler<DeleteTenantCommand, Result>
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

        tenantRepository.Remove(tenant);

        events.CollectEvent(new TenantDeleted(tenant.Id, tenant.State));

        return Result.Success();
    }
}
