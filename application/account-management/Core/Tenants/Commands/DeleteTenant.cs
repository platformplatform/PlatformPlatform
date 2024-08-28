using FluentValidation;
using PlatformPlatform.AccountManagement.Core.TelemetryEvents;
using PlatformPlatform.AccountManagement.Core.Tenants.Domain;
using PlatformPlatform.AccountManagement.Core.Users.Domain;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;
using PlatformPlatform.SharedKernel.ApplicationCore.TelemetryEvents;

namespace PlatformPlatform.AccountManagement.Core.Tenants.Commands;

public sealed record DeleteTenantCommand(TenantId Id) : ICommand, IRequest<Result>;

public sealed class DeleteTenantValidator : AbstractValidator<DeleteTenantCommand>
{
    public DeleteTenantValidator(IUserRepository userRepository)
    {
        RuleFor(x => x.Id)
            .MustAsync(async (tenantId, cancellationToken) =>
                await userRepository.CountTenantUsersAsync(tenantId, cancellationToken) == 0
            )
            .WithMessage("All users must be deleted before the tenant can be deleted.");
    }
}

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
