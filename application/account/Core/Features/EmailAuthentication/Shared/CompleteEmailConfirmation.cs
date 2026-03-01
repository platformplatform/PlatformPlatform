using PlatformPlatform.Account.Features.EmailAuthentication.Domain;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.Account.Features.EmailAuthentication.Shared;

public sealed record CompleteEmailConfirmationResponse(string Email, int ConfirmationTimeInSeconds);

public sealed class CompleteEmailConfirmation(
    IEmailLoginRepository emailLoginRepository,
    OneTimePasswordHelper oneTimePasswordHelper,
    ITelemetryEventsCollector events,
    TimeProvider timeProvider,
    ILogger<CompleteEmailConfirmation> logger
)
{
    public async Task<Result<CompleteEmailConfirmationResponse>> CompleteAsync(EmailLoginId id, string oneTimePassword, CancellationToken cancellationToken)
    {
        var emailLogin = await emailLoginRepository.GetByIdAsync(id, cancellationToken);

        if (emailLogin is null)
        {
            return Result<CompleteEmailConfirmationResponse>.NotFound($"Email login with id '{id}' not found.");
        }

        if (emailLogin.Completed)
        {
            logger.LogWarning("Email login with id '{EmailLoginId}' has already been completed", emailLogin.Id);
            return Result<CompleteEmailConfirmationResponse>.BadRequest($"Email login with id '{emailLogin.Id}' has already been completed.");
        }

        if (emailLogin.RetryCount >= EmailLogin.MaxAttempts)
        {
            emailLogin.RegisterInvalidPasswordAttempt();
            emailLoginRepository.Update(emailLogin);
            events.CollectEvent(new EmailLoginCodeBlocked(emailLogin.Id, emailLogin.Type, emailLogin.RetryCount));
            return Result<CompleteEmailConfirmationResponse>.Forbidden("Too many attempts, please request a new code.", true);
        }

        if (oneTimePasswordHelper.Validate(emailLogin.OneTimePasswordHash, oneTimePassword))
        {
            emailLogin.RegisterInvalidPasswordAttempt();
            emailLoginRepository.Update(emailLogin);
            events.CollectEvent(new EmailLoginCodeFailed(emailLogin.Id, emailLogin.Type, emailLogin.RetryCount));
            return Result<CompleteEmailConfirmationResponse>.BadRequest("The code is wrong or no longer valid.", true);
        }

        var confirmationTimeInSeconds = (int)(timeProvider.GetUtcNow() - emailLogin.CreatedAt).TotalSeconds;
        if (emailLogin.IsExpired(timeProvider.GetUtcNow()))
        {
            events.CollectEvent(new EmailLoginCodeExpired(emailLogin.Id, emailLogin.Type, confirmationTimeInSeconds));
            return Result<CompleteEmailConfirmationResponse>.BadRequest("The code is no longer valid, please request a new code.", true);
        }

        emailLogin.MarkAsCompleted(timeProvider.GetUtcNow());
        emailLoginRepository.Update(emailLogin);

        return new CompleteEmailConfirmationResponse(emailLogin.Email, confirmationTimeInSeconds);
    }
}
