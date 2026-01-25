using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using PlatformPlatform.Account.Features.Authentication.Domain;
using PlatformPlatform.Account.Features.EmailAuthentication.Domain;
using PlatformPlatform.Account.Features.EmailAuthentication.Shared;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.Account.Features.Users.Shared;
using PlatformPlatform.Account.Integrations.Gravatar;
using PlatformPlatform.SharedKernel.Authentication.TokenGeneration;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.Account.Features.EmailAuthentication.Commands;

[PublicAPI]
public sealed record CompleteEmailLoginCommand(string OneTimePassword, TenantId? PreferredTenantId = null) : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes this property from the API contract
    public EmailLoginId Id { get; init; } = null!;
}

public sealed class CompleteEmailLoginHandler(
    IUserRepository userRepository,
    ISessionRepository sessionRepository,
    UserInfoFactory userInfoFactory,
    AuthenticationTokenService authenticationTokenService,
    CompleteEmailConfirmation completeEmailConfirmation,
    AvatarUpdater avatarUpdater,
    GravatarClient gravatarClient,
    IHttpContextAccessor httpContextAccessor,
    IExecutionContext executionContext,
    ITelemetryEventsCollector events,
    TimeProvider timeProvider,
    ILogger<CompleteEmailLoginHandler> logger
) : IRequestHandler<CompleteEmailLoginCommand, Result>
{
    public async Task<Result> Handle(CompleteEmailLoginCommand command, CancellationToken cancellationToken)
    {
        var completeEmailConfirmationResult = await completeEmailConfirmation.CompleteAsync(
            command.Id, command.OneTimePassword, cancellationToken
        );

        if (!completeEmailConfirmationResult.IsSuccess) return Result.From(completeEmailConfirmationResult);

        var user = await userRepository.GetUserByEmailUnfilteredAsync(completeEmailConfirmationResult.Value!.Email, cancellationToken);
        if (user is null)
        {
            logger.LogWarning("User not found for email after completing email login '{EmailLoginId}'", command.Id);
            return Result.BadRequest("The code is wrong or no longer valid.");
        }

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

        var userAgent = httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString() ?? string.Empty;
        var ipAddress = executionContext.ClientIpAddress;

        var session = Session.Create(user.TenantId, user.Id, LoginMethod.OneTimePassword, userAgent, ipAddress);
        await sessionRepository.AddAsync(session, cancellationToken);

        user.UpdateLastSeen(timeProvider.GetUtcNow());
        userRepository.Update(user);

        var userInfo = await userInfoFactory.CreateUserInfoAsync(user, session.Id, cancellationToken);
        authenticationTokenService.CreateAndSetAuthenticationTokens(userInfo, session.Id, session.RefreshTokenJti);

        events.CollectEvent(new SessionCreated(session.Id));
        events.CollectEvent(new EmailLoginCompleted(user.Id, completeEmailConfirmationResult.Value!.ConfirmationTimeInSeconds));

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
