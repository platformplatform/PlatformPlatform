using FluentValidation;
using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Tenants.Domain;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.Account.Features.Tenants.Commands;

[PublicAPI]
public sealed record UpdateCurrentTenantCommand : ICommand, IRequest<Result>
{
    public required string Name { get; init; }
}

public sealed class UpdateCurrentTenantValidator : AbstractValidator<UpdateCurrentTenantCommand>
{
    public UpdateCurrentTenantValidator()
    {
        RuleFor(x => x.Name).Length(1, 30).WithMessage("Name must be between 1 and 30 characters.");
    }
}

public sealed class UpdateTenantHandler(
    ITenantRepository tenantRepository,
    IExecutionContext executionContext,
    ITelemetryEventsCollector events
) : IRequestHandler<UpdateCurrentTenantCommand, Result>
{
    public async Task<Result> Handle(UpdateCurrentTenantCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result.Forbidden("Only owners are allowed to update tenant information.");
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

        tenant.Update(command.Name);
        tenantRepository.Update(tenant);

        events.CollectEvent(new TenantUpdated());

        return Result.Success();
    }
}
