using FluentValidation;
using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.TelemetryEvents;
using PlatformPlatform.AccountManagement.Tenants.Domain;
using PlatformPlatform.AccountManagement.Users.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Entities;
using PlatformPlatform.SharedKernel.TelemetryEvents;

namespace PlatformPlatform.AccountManagement.Tenants.Commands;

[PublicAPI]
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
