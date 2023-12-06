using FluentValidation;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;
using PlatformPlatform.SharedKernel.ApplicationCore.TelemetryEvents;

namespace PlatformPlatform.AccountManagement.Application.Tenants;

public sealed record DeleteTenantCommand(TenantId Id) : ICommand, IRequest<Result>;

[UsedImplicitly]
public sealed class DeleteTenantHandler(ITenantRepository tenantRepository, ITelemetryEventsCollector events)
    : IRequestHandler<DeleteTenantCommand, Result>
{
    public async Task<Result> Handle(DeleteTenantCommand command, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdAsync(command.Id, cancellationToken);
        if (tenant is null) return Result.NotFound($"Tenant with id '{command.Id}' not found.");

        tenantRepository.Remove(tenant);

        events.CollectEvent(
            "TenantDeleted",
            new Dictionary<string, string>
            {
                { "Tenant_Id", tenant.Id.ToString() },
                { "Event_TenantState", tenant.State.ToString() }
            }
        );

        return Result.Success();
    }
}

[UsedImplicitly]
public sealed class DeleteTenantValidator : AbstractValidator<DeleteTenantCommand>
{
    public DeleteTenantValidator(IUserRepository userRepository)
    {
        RuleFor(x => x.Id)
            .MustAsync(async (tenantId, cancellationToken) =>
                await userRepository.CountTenantUsersAsync(tenantId, cancellationToken) == 0)
            .WithMessage("All users must be deleted before the tenant can be deleted.");
    }
}