using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Features.EmailConfirmations.Domain;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.AccountManagement.Features.EmailConfirmations.Commands;

[PublicAPI]
public sealed record CompleteEmailConfirmationCommand(EmailConfirmationId Id, string OneTimePassword)
    : ICommand, IRequest<Result<CompleteEmailConfirmationResponse>>;

[PublicAPI]
public sealed record CompleteEmailConfirmationResponse(string Email, int ConfirmationTimeInSeconds);

public sealed class CompleteEmailConfirmationHandler(
    IEmailConfirmationRepository emailConfirmationRepository,
    OneTimePasswordHelper oneTimePasswordHelper,
    ITelemetryEventsCollector events,
    ILogger<CompleteEmailConfirmationHandler> logger
) : IRequestHandler<CompleteEmailConfirmationCommand, Result<CompleteEmailConfirmationResponse>>
{
    public async Task<Result<CompleteEmailConfirmationResponse>> Handle(CompleteEmailConfirmationCommand command, CancellationToken cancellationToken)
    {
        var emailConfirmation = await emailConfirmationRepository.GetByIdAsync(command.Id, cancellationToken);

        if (emailConfirmation is null)
        {
            return Result<CompleteEmailConfirmationResponse>.NotFound($"Email confirmation with id '{command.Id}' not found.");
        }

        if (emailConfirmation.Completed)
        {
            logger.LogWarning("Email confirmation with id '{EmailConfirmationId}' has already been completed", emailConfirmation.Id);
            return Result<CompleteEmailConfirmationResponse>.BadRequest($"Email confirmation with id {emailConfirmation.Id} has already been completed.");
        }

        if (emailConfirmation.RetryCount >= EmailConfirmation.MaxAttempts)
        {
            emailConfirmation.RegisterInvalidPasswordAttempt();
            emailConfirmationRepository.Update(emailConfirmation);
            events.CollectEvent(new EmailConfirmationBlocked(emailConfirmation.Id, emailConfirmation.Type, emailConfirmation.RetryCount));
            return Result<CompleteEmailConfirmationResponse>.Forbidden("Too many attempts, please request a new code.", true);
        }

        if (oneTimePasswordHelper.Validate(emailConfirmation.OneTimePasswordHash, command.OneTimePassword))
        {
            emailConfirmation.RegisterInvalidPasswordAttempt();
            emailConfirmationRepository.Update(emailConfirmation);
            events.CollectEvent(new EmailConfirmationFailed(emailConfirmation.Id, emailConfirmation.Type, emailConfirmation.RetryCount));
            return Result<CompleteEmailConfirmationResponse>.BadRequest("The code is wrong or no longer valid.", true);
        }

        var confirmationTimeInSeconds = (int)(TimeProvider.System.GetUtcNow() - emailConfirmation.CreatedAt).TotalSeconds;
        if (emailConfirmation.HasExpired())
        {
            events.CollectEvent(new EmailConfirmationExpired(emailConfirmation.Id, emailConfirmation.Type, confirmationTimeInSeconds));
            return Result<CompleteEmailConfirmationResponse>.BadRequest("The code is no longer valid, please request a new code.", true);
        }

        emailConfirmation.MarkAsCompleted();
        emailConfirmationRepository.Update(emailConfirmation);

        return new CompleteEmailConfirmationResponse(emailConfirmation.Email, confirmationTimeInSeconds);
    }
}
