using JetBrains.Annotations;
using Mapster;
using PlatformPlatform.AccountManagement.Features.Authentication.Domain;
using PlatformPlatform.AccountManagement.Features.EmailConfirmations.Commands;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Shared;
using PlatformPlatform.AccountManagement.Integrations.Gravatar;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Authentication.TokenGeneration;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.AccountManagement.Features.Authentication.Commands;

[PublicAPI]
public sealed record CompleteLoginCommand(string OneTimePassword) : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes this property from the API contract
    public LoginId Id { get; init; } = null!;
}

public sealed class CompleteLoginHandler(
    IUserRepository userRepository,
    ILoginRepository loginRepository,
    AuthenticationTokenService authenticationTokenService,
    IMediator mediator,
    AvatarUpdater avatarUpdater,
    GravatarClient gravatarClient,
    ITelemetryEventsCollector events,
    ILogger<CompleteLoginHandler> logger
) : IRequestHandler<CompleteLoginCommand, Result>
{
    public async Task<Result> Handle(CompleteLoginCommand command, CancellationToken cancellationToken)
    {
        var login = await loginRepository.GetByIdAsync(command.Id, cancellationToken);

        if (login is null) return Result.NotFound($"Login with id '{command.Id}' not found.");

        if (login.Completed)
        {
            logger.LogWarning("Login with id '{LoginId}' has already been completed", login.Id);
            return Result.BadRequest($"The login process {login.Id} for user {login.UserId} has already been completed.");
        }

        var completeEmailConfirmationResult = await mediator.Send(
            new CompleteEmailConfirmationCommand(login.EmailConfirmationId, command.OneTimePassword),
            cancellationToken
        );

        if (!completeEmailConfirmationResult.IsSuccess) return Result.From(completeEmailConfirmationResult);

        var user = (await userRepository.GetByIdUnfilteredAsync(login.UserId, cancellationToken))!;

        if (!user.EmailConfirmed)
        {
            CompleteUserInvite(user);
        }

        if (user.Avatar.IsGravatar)
        {
            var gravatar = await gravatarClient.GetGravatar(user.Id, user.Email, cancellationToken);
            if (gravatar is not null)
            {
                if (await avatarUpdater.UpdateAvatar(user, true, gravatar.ContentType, gravatar.Stream, cancellationToken))
                {
                    events.CollectEvent(new GravatarUpdated(gravatar.Stream.Length));
                }
            }
        }

        login.MarkAsCompleted();
        loginRepository.Update(login);

        authenticationTokenService.CreateAndSetAuthenticationTokens(user.Adapt<UserInfo>());

        events.CollectEvent(new LoginCompleted(user.Id, completeEmailConfirmationResult.Value!.ConfirmationTimeInSeconds));

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
