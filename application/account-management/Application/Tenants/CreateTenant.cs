using FluentValidation;
using PlatformPlatform.AccountManagement.Application.TelemetryEvents;
using PlatformPlatform.AccountManagement.Domain.AccountRegistrations;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;
using PlatformPlatform.SharedKernel.ApplicationCore.TelemetryEvents;

namespace PlatformPlatform.AccountManagement.Application.Tenants;

public sealed record CreateTenantCommand(string AccountRegistrationId, string Subdomain, string Name)
    : ICommand, ITenantValidation, IRequest<Result<TenantId>>
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

        if (!accountRegistration.EmailConfirmedAt.HasValue)
        {
            logger.LogWarning(
                "AccountRegistration with id '{command.AccountRegistrationId}' has already been confirmed.",
                accountRegistration.Id);
            return Result<TenantId>.BadRequest("The email has not been confirmed.");
        }

        if (accountRegistration.CompletedAt.HasValue)
        {
            logger.LogWarning(
                "AccountRegistration with id '{command.AccountRegistrationId}' has already been completed.",
                accountRegistration.Id);
            return Result<TenantId>.BadRequest(
                $"The account registration {accountRegistration.Id} has already been completed.");
        }

        var tenant = Tenant.Create(
            command.Subdomain,
            command.Name,
            accountRegistration.Email,
            accountRegistration.FirstName,
            accountRegistration.LastName
        );
        await tenantRepository.AddAsync(tenant, cancellationToken);

        accountRegistration.MarkAsCompleted(tenant.Id);
        accountRegistrationRepository.Update(accountRegistration);

        var timeFromCreation = accountRegistration.CompletedAt!.Value - accountRegistration.CreatedAt;
        events.CollectEvent(new TenantCreated(tenant.Id, tenant.State, (int)timeFromCreation.TotalSeconds));

        return tenant.Id;
    }
}

[UsedImplicitly]
public sealed class CreateTenantValidator : TenantValidator<CreateTenantCommand>
{
    public CreateTenantValidator(ITenantRepository tenantRepository)
    {
        RuleFor(x => x.Subdomain).NotEmpty();
        RuleFor(x => x.Subdomain)
            .Matches("^[a-z0-9]{3,30}$")
            .WithMessage("Subdomain must be between 3-30 alphanumeric and lowercase characters.")
            .MustAsync(tenantRepository.IsSubdomainFreeAsync)
            .WithMessage("The subdomain is not available.")
            .When(x => !string.IsNullOrEmpty(x.Subdomain));
    }
}