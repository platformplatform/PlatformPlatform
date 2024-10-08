using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Authentication.Domain;
using PlatformPlatform.AccountManagement.Authentication.Services;
using PlatformPlatform.AccountManagement.TelemetryEvents;
using PlatformPlatform.AccountManagement.Users.Domain;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.TelemetryEvents;

namespace PlatformPlatform.AccountManagement.Authentication.Commands;

[PublicAPI]
public sealed record CompleteLoginCommand(string OneTimePassword) : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes this property from the API contract
    public LoginId Id { get; init; } = null!;
}

public sealed class CompleteLoginHandler(
    IUserRepository userRepository,
    ILoginRepository loginRepository,
    OneTimePasswordHelper oneTimePasswordHelper,
    AuthenticationTokenService authenticationTokenService,
    ITelemetryEventsCollector events,
    ILogger<CompleteLoginHandler> logger
) : IRequestHandler<CompleteLoginCommand, Result>
{
    public async Task<Result> Handle(CompleteLoginCommand command, CancellationToken cancellationToken)
    {
        var login = await loginRepository.GetByIdAsync(command.Id, cancellationToken);

        if (login is null)
        {
            return Result.NotFound($"Login with id '{command.Id}' not found.");
        }

        if (login.Completed)
        {
            logger.LogWarning("Login with id '{LoginId}' has already been completed.", login.Id);
            return Result.BadRequest($"The login process {login.Id} for user {login.UserId} has already been completed.");
        }

        if (login.RetryCount >= Login.MaxAttempts)
        {
            login.RegisterInvalidPasswordAttempt();
            loginRepository.Update(login);
            events.CollectEvent(new LoginBlocked(login.RetryCount));
            return Result.Forbidden("To many attempts, please request a new code.", true);
        }

        if (oneTimePasswordHelper.Validate(login.OneTimePasswordHash, command.OneTimePassword))
        {
            login.RegisterInvalidPasswordAttempt();
            loginRepository.Update(login);
            events.CollectEvent(new LoginFailed(login.RetryCount));
            return Result.BadRequest("The code is wrong or no longer valid.", true);
        }

        var loginTimeInSeconds = (int)(TimeProvider.System.GetUtcNow() - login.CreatedAt).TotalSeconds;
        if (login.HasExpired())
        {
            events.CollectEvent(new LoginExpired(loginTimeInSeconds));
            return Result.BadRequest("The code is no longer valid, please request a new code.", true);
        }

        var user = (await userRepository.GetByIdUnfilteredAsync(login.UserId, cancellationToken))!;

        if (!user.EmailConfirmed)
        {
            CompleteUserInvite(user);
        }

        login.MarkAsCompleted();
        loginRepository.Update(login);

        authenticationTokenService.CreateAndSetAuthenticationTokens(user);

        events.CollectEvent(new LoginCompleted(user.Id, loginTimeInSeconds));

        return Result.Success();
    }

    private void CompleteUserInvite(User user)
    {
        user.ConfirmEmail();
        userRepository.Update(user);
        var inviteAcceptedTimeInMinutes = (int)(TimeProvider.System.GetUtcNow() - user.CreatedAt).TotalMinutes;
        events.CollectEvent(new UserInviteAccepted(user.Id, inviteAcceptedTimeInMinutes));
    }
}
