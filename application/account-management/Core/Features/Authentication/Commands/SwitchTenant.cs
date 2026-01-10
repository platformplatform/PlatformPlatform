using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.AccountManagement.Features.Authentication.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Shared;
using PlatformPlatform.SharedKernel.Authentication.TokenGeneration;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Integrations.BlobStorage;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.AccountManagement.Features.Authentication.Commands;

[PublicAPI]
public sealed record SwitchTenantCommand(TenantId TenantId) : ICommand, IRequest<Result>;

public sealed class SwitchTenantHandler(
    IUserRepository userRepository,
    ISessionRepository sessionRepository,
    UserInfoFactory userInfoFactory,
    AuthenticationTokenService authenticationTokenService,
    IHttpContextAccessor httpContextAccessor,
    AvatarUpdater avatarUpdater,
    [FromKeyedServices("account-management-storage")]
    IBlobStorageClient blobStorageClient,
    IExecutionContext executionContext,
    ITelemetryEventsCollector events,
    TimeProvider timeProvider,
    ILogger<SwitchTenantHandler> logger
) : IRequestHandler<SwitchTenantCommand, Result>
{
    public async Task<Result> Handle(SwitchTenantCommand command, CancellationToken cancellationToken)
    {
        var users = await userRepository.GetUsersByEmailUnfilteredAsync(executionContext.UserInfo.Email!, cancellationToken);
        var targetUser = users.SingleOrDefault(u => u.TenantId == command.TenantId);
        if (targetUser is null)
        {
            logger.LogWarning("UserId '{UserId}' does not have access to TenantId '{TenantId}'", executionContext.UserInfo.Id, command.TenantId);
            return Result.Forbidden($"User does not have access to tenant '{command.TenantId}'.");
        }

        // If the user's email is not confirmed, confirm it and copy profile data from current user
        if (!targetUser.EmailConfirmed)
        {
            await CopyProfileDataFromCurrentUser(targetUser, cancellationToken);
        }

        var currentSessionId = executionContext.UserInfo.SessionId;
        var currentSession = await sessionRepository.GetByIdUnfilteredAsync(currentSessionId!, cancellationToken);
        if (currentSession is null)
        {
            logger.LogWarning("Current session '{SessionId}' not found", currentSessionId);
            return Result.Unauthorized("Current session not found.");
        }

        if (currentSession.IsRevoked)
        {
            logger.LogWarning("Current session '{SessionId}' is already revoked", currentSessionId);
            return Result.Unauthorized("Session has been revoked.");
        }

        var now = timeProvider.GetUtcNow();
        currentSession.Revoke(now, SessionRevokedReason.SwitchTenant);
        sessionRepository.Update(currentSession);
        events.CollectEvent(new SessionRevoked(SessionRevokedReason.SwitchTenant));

        var userAgent = httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString() ?? string.Empty;
        var ipAddress = executionContext.ClientIpAddress;

        var session = Session.Create(targetUser.TenantId, targetUser.Id, userAgent, ipAddress);
        await sessionRepository.AddAsync(session, cancellationToken);

        targetUser.UpdateLastSeen(timeProvider.GetUtcNow());
        userRepository.Update(targetUser);

        var userInfo = await userInfoFactory.CreateUserInfoAsync(targetUser, session.Id, cancellationToken);
        authenticationTokenService.SwitchTenantAndSetAuthenticationTokens(userInfo, session.Id, session.RefreshTokenJti, currentSession.ExpiresAt);

        events.CollectEvent(new SessionCreated(session.Id));
        events.CollectEvent(new TenantSwitched(executionContext.TenantId!, command.TenantId, targetUser.Id));

        return Result.Success();
    }

    private async Task CopyProfileDataFromCurrentUser(User targetUser, CancellationToken cancellationToken)
    {
        // Get the current user to copy profile data from
        var currentUser = await userRepository.GetByIdUnfilteredAsync(executionContext.UserInfo.Id!, cancellationToken);
        targetUser.Update(
            currentUser!.FirstName ?? targetUser.FirstName ?? "",
            currentUser.LastName ?? targetUser.LastName ?? "",
            currentUser.Title ?? targetUser.Title ?? ""
        );

        // Copy locale (preferred language)
        targetUser.ChangeLocale(currentUser.Locale);

        // Copy avatar if the current user has one
        if (targetUser.Avatar.Url is null && currentUser.Avatar.Url?.StartsWith("/avatars/") == true)
        {
            // Blob-stored avatar - copy the blob to the new tenant
            var sourceBlobPath = currentUser.Avatar.Url[9..]; // Skip "/avatars/" prefix

            // Download the avatar blob from the source tenant
            var avatarData = await blobStorageClient.DownloadAsync("avatars", sourceBlobPath, cancellationToken);
            if (avatarData is not null)
            {
                // Copy to MemoryStream since Azure's RetriableStream doesn't support seeking (Position reset)
                await using var avatarStream = avatarData.Value.Stream;
                using var memoryStream = new MemoryStream();
                await avatarStream.CopyToAsync(memoryStream, cancellationToken);
                memoryStream.Position = 0;

                // Upload the avatar to the target tenant's storage location
                await avatarUpdater.UpdateAvatar(targetUser, false, avatarData.Value.ContentType, memoryStream, cancellationToken);
            }
        }

        targetUser.ConfirmEmail();
        userRepository.Update(targetUser);

        // Calculate how long it took to accept the invitation
        var inviteAcceptedTimeInMinutes = (int)(timeProvider.GetUtcNow() - targetUser.CreatedAt).TotalMinutes;
        events.CollectEvent(new UserInviteAccepted(targetUser.Id, inviteAcceptedTimeInMinutes));
    }
}
