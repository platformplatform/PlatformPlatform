using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Features.Tenants.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.AccountManagement.Features.Tenants.Commands;

[PublicAPI]
public sealed record RemoveTenantLogoCommand : ICommand, IRequest<Result>;

public sealed class RemoveTenantLogoHandler(
    ITenantRepository tenantRepository,
    IExecutionContext executionContext,
    ITelemetryEventsCollector events
)
    : IRequestHandler<RemoveTenantLogoCommand, Result>
{
    public async Task<Result> Handle(RemoveTenantLogoCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role != UserRole.Owner.ToString())
        {
            return Result.Forbidden("Only owners are allowed to remove tenant logo.");
        }

        var tenant = await tenantRepository.GetCurrentTenantAsync(cancellationToken);

        tenant.RemoveLogo();
        tenantRepository.Update(tenant);

        events.CollectEvent(new TenantLogoRemoved());

        return Result.Success();
    }
}
