using PlatformPlatform.AccountManagement.Features.Tenants.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Commands;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.AccountManagement.Features.Tenants.Commands;

internal sealed record CreateTenantCommand(string OwnerEmail, bool EmailConfirmed, string? Locale)
    : ICommand, IRequest<Result<CreateTenantResponse>>;

internal sealed record CreateTenantResponse(TenantId TenantId, UserId UserId);

internal sealed class CreateTenantHandler(ITenantRepository tenantRepository, IMediator mediator, ITelemetryEventsCollector events)
    : IRequestHandler<CreateTenantCommand, Result<CreateTenantResponse>>
{
    public async Task<Result<CreateTenantResponse>> Handle(CreateTenantCommand command, CancellationToken cancellationToken)
    {
        var tenant = Tenant.Create(command.OwnerEmail);
        await tenantRepository.AddAsync(tenant, cancellationToken);

        events.CollectEvent(new TenantCreated(tenant.Id, tenant.State));

        var createUserResult = await mediator.Send(
            new CreateUserCommand(tenant.Id, command.OwnerEmail, UserRole.Owner, command.EmailConfirmed, command.Locale),
            cancellationToken
        );

        return new CreateTenantResponse(tenant.Id, createUserResult.Value!);
    }
}
