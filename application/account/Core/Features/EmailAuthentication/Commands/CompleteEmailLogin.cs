using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using PlatformPlatform.Account.Features.Authentication.Domain;
using PlatformPlatform.Account.Features.EmailAuthentication.Domain;
using PlatformPlatform.Account.Features.EmailAuthentication.Shared;
using PlatformPlatform.Account.Features.Tenants.Domain;
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
    ITenantRepository tenantRepository,
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

        var allUsers = await userRepository.GetUsersByEmailUnfilteredAsync(completeEmailConfirmationResult.Value!.Email, cancellationToken);
        var activeTenantIds = (await tenantRepository.GetByIdsAsync(allUsers.Select(u => u.TenantId).Distinct().ToArray(), cancellationToken))
            .Select(t => t.Id).ToHashSet();
        var activeUsers = allUsers.Where(u => activeTenantIds.Contains(u.TenantId)).ToArray();

        if (activeUsers.Length == 0)
        {
            logger.LogWarning("No active users found for email after completing email login '{EmailLoginId}'", command.Id);
            return Result.BadRequest("The code is wrong or no longer valid.");
        }

        var user = command.PreferredTenantId is not null
            ? activeUsers.SingleOrDefault(u => u.TenantId == command.PreferredTenantId) ?? activeUsers[0]
            : activeUsers[0];

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

        var userInfoResult = await userInfoFactory.CreateUserInfoAsync(user, session.Id, cancellationToken);
        if (!userInfoResult.IsSuccess) return Result.From(userInfoResult);

        authenticationTokenService.CreateAndSetAuthenticationTokens(userInfoResult.Value!, session.Id, session.RefreshTokenJti);

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
