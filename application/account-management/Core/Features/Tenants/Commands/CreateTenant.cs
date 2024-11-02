using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Features.Tenants.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Commands;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.AccountManagement.TelemetryEvents;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.TelemetryEvents;

namespace PlatformPlatform.AccountManagement.Features.Tenants.Commands;

[PublicAPI]
public sealed record CreateTenantCommand(TenantId Id, string OwnerEmail, bool EmailConfirmed)
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
            new CreateUserCommand(tenant.Id, command.OwnerEmail, UserRole.Owner, command.EmailConfirmed)
            , cancellationToken
        );

        return result.Value!;
    }
}
