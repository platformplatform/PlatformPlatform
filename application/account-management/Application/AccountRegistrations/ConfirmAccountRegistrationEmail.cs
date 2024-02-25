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

        if (accountRegistration.OneTimePassword != command.OneTimePassword)
        {
            accountRegistration.RegisterInvalidPasswordAttempt();
            accountRegistrationRepository.Update(accountRegistration);
            events.CollectEvent(new AccountRegistrationEmailConfirmationAttemptFailed(accountRegistration.RetryCount));
            return Result.BadRequest("The code is wrong or no longer valid.", true);
        }

        if (accountRegistration.RetryCount >= AccountRegistration.MaxAttempts)
        {
            events.CollectEvent(new AccountRegistrationEmailConfirmedButBlocked(accountRegistration.RetryCount));
            return Result.Forbidden("To many attempts, please request a new code.", true);
        }

        if (accountRegistration.HasExpired())
        {
            var timeFromCreation = TimeProvider.System.GetUtcNow() - accountRegistration.CreatedAt;
            events.CollectEvent(new AccountRegistrationEmailConfirmedButExpired((int)timeFromCreation.TotalSeconds));
            return Result.BadRequest("The code is no longer valid, please request a new code.", true);
        }

        if (accountRegistration.EmailConfirmedAt.HasValue)
        {
            logger.LogWarning("AccountRegistration with id '{AccountRegistrationId}' has already been confirmed.",
                command.Id);
            return Result.BadRequest("The code has already been used, please request a new code.");
        }

        accountRegistration.ConfirmEmail();
        accountRegistrationRepository.Update(accountRegistration);
        events.CollectEvent(new AccountRegistrationEmailConfirmed());

        return Result.Success();
    }
}