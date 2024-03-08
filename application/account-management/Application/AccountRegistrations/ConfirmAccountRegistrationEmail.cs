using PlatformPlatform.AccountManagement.Application.TelemetryEvents;
using PlatformPlatform.AccountManagement.Domain.AccountRegistrations;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;
using PlatformPlatform.SharedKernel.ApplicationCore.TelemetryEvents;

namespace PlatformPlatform.AccountManagement.Application.AccountRegistrations;

[UsedImplicitly]
public sealed record ConfirmAccountRegistrationEmailCommand : ICommand, IRequest<Result>
{
    [JsonIgnore]
    public AccountRegistrationId Id { get; init; } = null!;

    [UsedImplicitly]
    public required string OneTimePassword { get; init; }
}

[UsedImplicitly]
public sealed class ConfirmAccountRegistrationEmailCommandHandler(
    IAccountRegistrationRepository accountRegistrationRepository,
    ITenantRepository tenantRepository,
    ITelemetryEventsCollector events,
    ILogger<ConfirmAccountRegistrationEmailCommandHandler> logger
)
    : IRequestHandler<ConfirmAccountRegistrationEmailCommand, Result>
{
    public async Task<Result> Handle(
        ConfirmAccountRegistrationEmailCommand command,
        CancellationToken cancellationToken
    )
    {
        var accountRegistration = await accountRegistrationRepository.GetByIdAsync(command.Id, cancellationToken);

        if (accountRegistration is null)
        {
            return Result.NotFound($"AccountRegistration with id '{command.Id}' not found.");
        }

        var timeFromCreation = TimeProvider.System.GetUtcNow() - accountRegistration.CreatedAt;

        if (accountRegistration.OneTimePassword != command.OneTimePassword)
        {
            accountRegistration.RegisterInvalidPasswordAttempt();
            accountRegistrationRepository.Update(accountRegistration);
            events.CollectEvent(new AccountRegistrationEmailConfirmationAttemptFailed(accountRegistration.RetryCount));
            return Result.BadRequest("The code is wrong or no longer valid.", true);
        }

        if (accountRegistration.Completed)
        {
            logger.LogWarning(
                "AccountRegistration with id '{AccountRegistrationId}' has already been completed.",
                accountRegistration.Id);
            return Result.BadRequest(
                $"The account registration {accountRegistration.Id} for tenant {accountRegistration.TenantId} has already been completed.");
        }

        if (accountRegistration.RetryCount >= AccountRegistration.MaxAttempts)
        {
            events.CollectEvent(new AccountRegistrationEmailConfirmedButBlocked(accountRegistration.RetryCount));
            return Result.Forbidden("To many attempts, please request a new code.", true);
        }

        if (accountRegistration.HasExpired())
        {
            events.CollectEvent(new AccountRegistrationEmailConfirmedButExpired((int)timeFromCreation.TotalSeconds));
            return Result.BadRequest("The code is no longer valid, please request a new code.", true);
        }

        var tenant = Tenant.Create(accountRegistration.TenantId, accountRegistration.Email);
        await tenantRepository.AddAsync(tenant, cancellationToken);

        accountRegistration.MarkAsCompleted();
        accountRegistrationRepository.Update(accountRegistration);

        events.CollectEvent(new TenantCreated(tenant.Id, tenant.State, (int)timeFromCreation.TotalSeconds));

        return Result.Success();
    }
}