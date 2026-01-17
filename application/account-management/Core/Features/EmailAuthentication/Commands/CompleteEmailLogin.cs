using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using PlatformPlatform.AccountManagement.Features.Authentication.Domain;
using PlatformPlatform.AccountManagement.Features.EmailAuthentication.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Shared;
using PlatformPlatform.AccountManagement.Integrations.Gravatar;
using PlatformPlatform.SharedKernel.Authentication.TokenGeneration;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.AccountManagement.Features.EmailAuthentication.Commands;

[PublicAPI]
public sealed record CompleteEmailLoginCommand(string OneTimePassword, TenantId? PreferredTenantId = null) : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes this property from the API contract
    public EmailLoginId Id { get; init; } = null!;
}

public sealed class CompleteEmailLoginHandler(
    IUserRepository userRepository,
    IEmailLoginRepository emailLoginRepository,
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
    ILogger<CompleteEmailLoginHandler> logger
) : IRequestHandler<CompleteEmailLoginCommand, Result>
{
    public async Task<Result> Handle(CompleteEmailLoginCommand command, CancellationToken cancellationToken)
    {
        var emailLogin = await emailLoginRepository.GetByIdAsync(command.Id, cancellationToken);
        if (emailLogin is null)
        {
            // For security, avoid confirming the existence of email login IDs
            return Result.BadRequest("The code is wrong or no longer valid.");
        }

        if (emailLogin.Completed)
        {
            logger.LogWarning("Email login with id '{EmailLoginId}' has already been completed", emailLogin.Id);
            return Result.BadRequest($"The email login process '{emailLogin.Id}' for user '{emailLogin.UserId}' has already been completed.");
        }

        var completeEmailConfirmationResult = await mediator.Send(
            new CompleteEmailConfirmationCommand(emailLogin.EmailConfirmationId, command.OneTimePassword),
            cancellationToken
        );

        if (!completeEmailConfirmationResult.IsSuccess) return Result.From(completeEmailConfirmationResult);

        var user = (await userRepository.GetByIdUnfilteredAsync(emailLogin.UserId, cancellationToken))!;

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

        emailLogin.MarkAsCompleted();
        emailLoginRepository.Update(emailLogin);

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
