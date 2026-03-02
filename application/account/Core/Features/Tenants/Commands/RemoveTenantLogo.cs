using Account.Features.Tenants.Domain;
using Account.Features.Users.Domain;
using JetBrains.Annotations;
using SharedKernel.Authentication;
using SharedKernel.Cqrs;
using SharedKernel.ExecutionContext;
using SharedKernel.Telemetry;

namespace Account.Features.Tenants.Commands;

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
