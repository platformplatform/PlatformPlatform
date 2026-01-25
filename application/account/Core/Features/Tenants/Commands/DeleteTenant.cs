using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Tenants.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.Account.Features.Tenants.Commands;

[PublicAPI]
public sealed record DeleteTenantCommand(TenantId Id) : ICommand, IRequest<Result>;

public sealed class DeleteTenantHandler(ITenantRepository tenantRepository, ITelemetryEventsCollector events)
    : IRequestHandler<DeleteTenantCommand, Result>
{
    public async Task<Result> Handle(DeleteTenantCommand command, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdAsync(command.Id, cancellationToken);
        if (tenant is null) return Result.NotFound($"Tenant with id '{command.Id}' not found.");

        tenantRepository.Remove(tenant);

        events.CollectEvent(new TenantDeleted(tenant.Id, tenant.State));

        return Result.Success();
    }
}
