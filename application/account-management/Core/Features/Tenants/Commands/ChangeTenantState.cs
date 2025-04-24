using FluentValidation;
using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Features.Tenants.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.AccountManagement.Features.Tenants.Commands;

[PublicAPI]
public sealed record ChangeTenantStateCommand(TenantState NewState) : ICommand, IRequest<Result>;

public sealed class ChangeTenantStateValidator : AbstractValidator<ChangeTenantStateCommand>
{
    public ChangeTenantStateValidator()
    {
        RuleFor(x => x.NewState).IsInEnum().WithMessage("Invalid tenant state provided.");
    }
}

public sealed class ChangeTenantStateHandler(ITenantRepository tenantRepository, IExecutionContext executionContext, ITelemetryEventsCollector events)
    : IRequestHandler<ChangeTenantStateCommand, Result>
{
    public async Task<Result> Handle(ChangeTenantStateCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role != UserRole.Owner.ToString())
        {
            return Result.Forbidden("Only owners are allowed to change tenant state.");
        }

        var tenant = await tenantRepository.GetByIdAsync(executionContext.TenantId!, cancellationToken);
        if (tenant is null)
        {
            return Result.NotFound($"Tenant with ID '{executionContext.TenantId}' not found.");
        }

        var oldTenantState = tenant.State;
        if (command.NewState == oldTenantState)
        {
            return Result.BadRequest($"Tenant is already in state '{command.NewState}'.");
        }

        tenant.ChangeState(command.NewState, executionContext.UserInfo.Email!);
        tenantRepository.Update(tenant);

        events.CollectEvent(new TenantStateChanged(tenant.Id, command.NewState, oldTenantState));

        return Result.Success();
    }
}
