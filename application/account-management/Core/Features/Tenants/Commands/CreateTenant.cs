using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Features.Tenants.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Commands;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.AccountManagement.Features.Tenants.Commands;

[PublicAPI]
public sealed record CreateTenantCommand(TenantId Id, string OwnerEmail, bool EmailConfirmed, string? Locale)
    : ICommand, IRequest<Result<UserId>>;

public sealed class CreateTenantHandler(ITenantRepository tenantRepository, IMediator mediator, ITelemetryEventsCollector events)
    : IRequestHandler<CreateTenantCommand, Result<UserId>>
{
    public async Task<Result<UserId>> Handle(CreateTenantCommand command, CancellationToken cancellationToken)
    {
        var tenant = Tenant.Create(command.Id, command.OwnerEmail);
        await tenantRepository.AddAsync(tenant, cancellationToken);

        events.CollectEvent(new TenantCreated(tenant.Id, tenant.State));

        var result = await mediator.Send(
            new CreateUserCommand(command.OwnerEmail, UserRole.Owner, command.EmailConfirmed, command.Locale) { TenantId = command.Id },
            cancellationToken
        );

        return result.Value!;
    }
}
