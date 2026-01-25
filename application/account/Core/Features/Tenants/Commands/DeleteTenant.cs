using FluentValidation;
using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Tenants.Domain;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.Account.Features.Tenants.Commands;

[PublicAPI]
public sealed record DeleteTenantCommand(TenantId Id) : ICommand, IRequest<Result>;

public sealed class DeleteTenantValidator : AbstractValidator<DeleteTenantCommand>
{
    public DeleteTenantValidator(IUserRepository userRepository)
    {
        RuleFor(x => x.Id)
            .MustAsync(async (tenantId, cancellationToken) =>
                await userRepository.CountTenantUsersAsync(tenantId, cancellationToken) <= 1
            )
            .WithMessage("All users must be deleted before the tenant can be deleted.");
    }
}

public sealed class DeleteTenantHandler(ITenantRepository tenantRepository, IUserRepository userRepository, ITelemetryEventsCollector events)
    : IRequestHandler<DeleteTenantCommand, Result>
{
    public async Task<Result> Handle(DeleteTenantCommand command, CancellationToken cancellationToken)
    {
        var tenant = await tenantRepository.GetByIdAsync(command.Id, cancellationToken);
        if (tenant is null) return Result.NotFound($"Tenant with id '{command.Id}' not found.");

        var tenantUsers = await userRepository.GetTenantUsers(cancellationToken);
        userRepository.RemoveRange(tenantUsers);

        tenantRepository.Remove(tenant);

        events.CollectEvent(new TenantDeleted(tenant.Id, tenant.State, tenantUsers.Length));

        return Result.Success();
    }
}
