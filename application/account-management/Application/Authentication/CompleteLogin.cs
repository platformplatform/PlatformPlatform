using PlatformPlatform.AccountManagement.Application.TelemetryEvents;
using PlatformPlatform.AccountManagement.Domain.Authentication;
using PlatformPlatform.SharedKernel.ApplicationCore.Authentication;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;
using PlatformPlatform.SharedKernel.ApplicationCore.TelemetryEvents;

namespace PlatformPlatform.AccountManagement.Application.Authentication;

public sealed record CompleteLoginCommand(string OneTimePassword)
    : ICommand, IRequest<Result>
{
    [JsonIgnore]
    public LoginId Id { get; init; } = null!;
}

public sealed class CompleteLoginHandler(
    IUserRepository userRepository,
    ILoginRepository loginProcessRepository,
    OneTimePasswordValidator oneTimePasswordValidator,
    ITelemetryEventsCollector events,
    ILogger<CompleteLoginHandler> logger
) : IRequestHandler<CompleteLoginCommand, Result>
{
    public async Task<Result> Handle(CompleteLoginCommand command, CancellationToken cancellationToken)
    {
        var loginProcess = await loginProcessRepository.GetByIdAsync(command.Id, cancellationToken);

        if (loginProcess is null)
        {
            return Result.NotFound($"Login with id '{command.Id}' not found.");
        }

        if (oneTimePasswordValidator.Validate(loginProcess.OneTimePasswordHash, command.OneTimePassword))
        {
            loginProcess.RegisterInvalidPasswordAttempt();
            loginProcessRepository.Update(loginProcess);
            events.CollectEvent(new LoginFailed(loginProcess.RetryCount));
            return Result.BadRequest("The code is wrong or no longer valid.", true);
        }

        if (loginProcess.Completed)
        {
            logger.LogWarning(
                "Login with id '{LoginId}' has already been completed.", loginProcess.Id
            );
            return Result.BadRequest(
                $"The login process {loginProcess.Id} for user {loginProcess.UserId} has already been completed."
            );
        }

        if (loginProcess.RetryCount >= Login.MaxAttempts)
        {
            events.CollectEvent(new LoginBlocked(loginProcess.RetryCount));
            return Result.Forbidden("To many attempts, please request a new code.", true);
        }

        var registrationTimeInSeconds = (TimeProvider.System.GetUtcNow() - loginProcess.CreatedAt).TotalSeconds;
        if (loginProcess.HasExpired())
        {
            events.CollectEvent(new LoginExpired((int)registrationTimeInSeconds));
            return Result.BadRequest("The code is no longer valid, please request a new code.", true);
        }

        var user = (await userRepository.GetByIdAsync(loginProcess.UserId, cancellationToken))!;

        loginProcess.MarkAsCompleted();
        loginProcessRepository.Update(loginProcess);

        // Create a new session for the user

        events.CollectEvent(new LoginCompleted(user.Id, (int)registrationTimeInSeconds));

        return Result.Success();
    }
}
