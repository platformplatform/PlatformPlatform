using Microsoft.AspNetCore.Identity;
using PlatformPlatform.AccountManagement.Application.TelemetryEvents;
using PlatformPlatform.AccountManagement.Domain.AccountRegistrations;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;
using PlatformPlatform.SharedKernel.ApplicationCore.TelemetryEvents;

namespace PlatformPlatform.AccountManagement.Application.AccountRegistrations;

public sealed record CompleteAccountRegistrationCommand(string OneTimePassword)
    : ICommand, IRequest<Result>
{
    [JsonIgnore]
    public AccountRegistrationId Id { get; init; } = null!;
}

public sealed class CompleteAccountRegistrationHandler(
    ITenantRepository tenantRepository,
    IAccountRegistrationRepository accountRegistrationRepository,
    IPasswordHasher<object> passwordHasher,
    ITelemetryEventsCollector events,
    ILogger<CompleteAccountRegistrationHandler> logger
) : IRequestHandler<CompleteAccountRegistrationCommand, Result>
{
    public async Task<Result> Handle(CompleteAccountRegistrationCommand command, CancellationToken cancellationToken)
    {
        var accountRegistration = await accountRegistrationRepository.GetByIdAsync(command.Id, cancellationToken);

        if (accountRegistration is null)
        {
            return Result.NotFound($"AccountRegistration with id '{command.Id}' not found.");
        }

        if (ValidateOneTimePassword(command, accountRegistration))
        {
            accountRegistration.RegisterInvalidPasswordAttempt();
            accountRegistrationRepository.Update(accountRegistration);
            events.CollectEvent(new AccountRegistrationFailed(accountRegistration.RetryCount));
            return Result.BadRequest("The code is wrong or no longer valid.", true);
        }

        if (accountRegistration.Completed)
        {
            logger.LogWarning(
                "AccountRegistration with id '{AccountRegistrationId}' has already been completed.", accountRegistration.Id
            );
            return Result.BadRequest(
                $"The account registration {accountRegistration.Id} for tenant {accountRegistration.TenantId} has already been completed."
            );
        }

        if (accountRegistration.RetryCount >= AccountRegistration.MaxAttempts)
        {
            events.CollectEvent(new AccountRegistrationBlocked(accountRegistration.RetryCount));
            return Result.Forbidden("To many attempts, please request a new code.", true);
        }

        var registrationTimeInSeconds = (TimeProvider.System.GetUtcNow() - accountRegistration.CreatedAt).TotalSeconds;
        if (accountRegistration.HasExpired())
        {
            events.CollectEvent(new AccountRegistrationExpired((int)registrationTimeInSeconds));
            return Result.BadRequest("The code is no longer valid, please request a new code.", true);
        }

        var tenant = Tenant.Create(accountRegistration.TenantId, accountRegistration.Email);
        await tenantRepository.AddAsync(tenant, cancellationToken);

        accountRegistration.MarkAsCompleted();
        accountRegistrationRepository.Update(accountRegistration);

        events.CollectEvent(new AccountRegistrationCompleted(tenant.Id, tenant.State, (int)registrationTimeInSeconds));

        return Result.Success();
    }

    private bool ValidateOneTimePassword(CompleteAccountRegistrationCommand command, AccountRegistration accountRegistration)
    {
        var passwordVerificationResult = passwordHasher.VerifyHashedPassword(this, accountRegistration.OneTimePasswordHash, command.OneTimePassword);

        OverRidePasswordVerificationResult(command.OneTimePassword, ref passwordVerificationResult);

        return passwordVerificationResult == PasswordVerificationResult.Failed;

        [Conditional("DEBUG")]
        static void OverRidePasswordVerificationResult(string oneTimePassword, ref PasswordVerificationResult passwordVerificationResult)
        {
            // When debugging, we can always use the "UNLOCK" code to verify the password
            if (oneTimePassword == "UNLOCK") passwordVerificationResult = PasswordVerificationResult.Success;
        }
    }
}
