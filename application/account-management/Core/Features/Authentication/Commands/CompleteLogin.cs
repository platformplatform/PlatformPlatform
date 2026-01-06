using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using PlatformPlatform.AccountManagement.Features.Authentication.Domain;
using PlatformPlatform.AccountManagement.Features.EmailConfirmations.Commands;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Shared;
using PlatformPlatform.AccountManagement.Integrations.Gravatar;
using PlatformPlatform.SharedKernel.Authentication.TokenGeneration;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.AccountManagement.Features.Authentication.Commands;

[PublicAPI]
public sealed record CompleteLoginCommand(string OneTimePassword, TenantId? PreferredTenantId = null) : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes this property from the API contract
    public LoginId Id { get; init; } = null!;
}

public sealed class CompleteLoginHandler(
    IUserRepository userRepository,
    ILoginRepository loginRepository,
    ISessionRepository sessionRepository,
    UserInfoFactory userInfoFactory,
    AuthenticationTokenService authenticationTokenService,
    IMediator mediator,
    AvatarUpdater avatarUpdater,
    GravatarClient gravatarClient,
    IHttpContextAccessor httpContextAccessor,
    IExecutionContext executionContext,
    ITelemetryEventsCollector events,
    TimeProvider timeProvider,
    ILogger<CompleteLoginHandler> logger
) : IRequestHandler<CompleteLoginCommand, Result>
{
    public async Task<Result> Handle(CompleteLoginCommand command, CancellationToken cancellationToken)
    {
        var login = await loginRepository.GetByIdAsync(command.Id, cancellationToken);
        if (login is null)
        {
            // For security, avoid confirming the existence of login IDs
            return Result.BadRequest("The code is wrong or no longer valid.");
        }

        if (login.Completed)
        {
            logger.LogWarning("Login with id '{LoginId}' has already been completed", login.Id);
            return Result.BadRequest($"The login process '{login.Id}' for user '{login.UserId}' has already been completed.");
        }

        var completeEmailConfirmationResult = await mediator.Send(
            new CompleteEmailConfirmationCommand(login.EmailConfirmationId, command.OneTimePassword),
            cancellationToken
        );

        if (!completeEmailConfirmationResult.IsSuccess) return Result.From(completeEmailConfirmationResult);

        var user = (await userRepository.GetByIdUnfilteredAsync(login.UserId, cancellationToken))!;

        // Check if PreferredTenantId is provided and valid
        if (command.PreferredTenantId is not null)
        {
            var usersWithSameEmail = await userRepository.GetUsersByEmailUnfilteredAsync(user.Email, cancellationToken);
            var preferredTenantUser = usersWithSameEmail.SingleOrDefault(u => u.TenantId == command.PreferredTenantId);

            if (preferredTenantUser is not null)
            {
                user = preferredTenantUser;
            }
        }

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

        var userAgent = httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString() ?? string.Empty;
        var ipAddress = executionContext.ClientIpAddress;

        var session = Session.Create(user.TenantId, user.Id, userAgent, ipAddress);
        await sessionRepository.AddAsync(session, cancellationToken);

        var userInfo = await userInfoFactory.CreateUserInfoAsync(user, cancellationToken, session.Id);
        authenticationTokenService.CreateAndSetAuthenticationTokens(userInfo, session.Id, session.RefreshTokenJti);

        events.CollectEvent(new SessionCreated(session.Id));
        events.CollectEvent(new LoginCompleted(user.Id, completeEmailConfirmationResult.Value!.ConfirmationTimeInSeconds));

        return Result.Success();
    }

    private void CompleteUserInvite(User user)
    {
        user.ConfirmEmail();
        userRepository.Update(user);
        var inviteAcceptedTimeInMinutes = (int)(timeProvider.GetUtcNow() - user.CreatedAt).TotalMinutes;
        events.CollectEvent(new UserInviteAccepted(user.Id, inviteAcceptedTimeInMinutes));
    }
}
