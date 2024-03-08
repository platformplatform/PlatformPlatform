using PlatformPlatform.AccountManagement.Application.TelemetryEvents;
using PlatformPlatform.AccountManagement.Domain.AccountRegistrations;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;
using PlatformPlatform.SharedKernel.ApplicationCore.TelemetryEvents;

namespace PlatformPlatform.AccountManagement.Application.Tenants;

public sealed record CreateTenantCommand(string AccountRegistrationId)
    : ICommand, IRequest<Result<TenantId>>
{
    public AccountRegistrationId GetAccountRegistrationId()
    {
        return new AccountRegistrationId(AccountRegistrationId);
    }
}

[UsedImplicitly]
public sealed class CreateTenantHandler(
    ITenantRepository tenantRepository,
    IAccountRegistrationRepository accountRegistrationRepository,
    ITelemetryEventsCollector events,
    ILogger<CreateTenantHandler> logger
) : IRequestHandler<CreateTenantCommand, Result<TenantId>>
{
    public async Task<Result<TenantId>> Handle(CreateTenantCommand command, CancellationToken cancellationToken)
    {
        var accountRegistration =
            await accountRegistrationRepository.GetByIdAsync(command.GetAccountRegistrationId(), cancellationToken);

        if (accountRegistration is null)
        {
            return Result<TenantId>.NotFound(
                $"AccountRegistration with id '{command.GetAccountRegistrationId()}' not found.");
        }

        if (accountRegistration.Completed)
        {
            logger.LogWarning(
                "AccountRegistration with id '{command.AccountRegistrationId}' has already been completed.",
                accountRegistration.Id);
            return Result<TenantId>.BadRequest(
                $"The account registration {accountRegistration.Id} for tenant {accountRegistration.TenantId} has already been completed.");
        }

        var tenant = Tenant.Create(accountRegistration.TenantId, accountRegistration.Email);
        await tenantRepository.AddAsync(tenant, cancellationToken);

        accountRegistration.MarkAsCompleted();
        accountRegistrationRepository.Update(accountRegistration);

        var timeFromCreation = TimeProvider.System.GetUtcNow() - accountRegistration.CreatedAt;
        events.CollectEvent(new TenantCreated(tenant.Id, tenant.State, (int)timeFromCreation.TotalSeconds));

        return tenant.Id;
    }
}