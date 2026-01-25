using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Tenants.Domain;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.Account.Features.Tenants.Commands;

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
        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result.Forbidden("Only owners are allowed to remove tenant logo.");
        }

        var tenant = await tenantRepository.GetCurrentTenantAsync(cancellationToken);
        if (tenant is null)
        {
            return Result.Unauthorized("Tenant has been deleted.", responseHeaders: new Dictionary<string, string>
                {
                    { AuthenticationTokenHttpKeys.UnauthorizedReasonHeaderKey, nameof(UnauthorizedReason.TenantDeleted) }
                }
            );
        }

        tenant.RemoveLogo();
        tenantRepository.Update(tenant);

        events.CollectEvent(new TenantLogoRemoved());

        return Result.Success();
    }
}
