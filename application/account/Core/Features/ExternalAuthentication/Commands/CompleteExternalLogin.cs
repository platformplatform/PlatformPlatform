using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using PlatformPlatform.Account.Features.Authentication.Domain;
using PlatformPlatform.Account.Features.ExternalAuthentication.Domain;
using PlatformPlatform.Account.Features.ExternalAuthentication.Shared;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.Account.Features.Users.Shared;
using PlatformPlatform.Account.Integrations.OAuth;
using PlatformPlatform.SharedKernel.Authentication.TokenGeneration;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.OpenIdConnect;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.Account.Features.ExternalAuthentication.Commands;

[PublicAPI]
public sealed record CompleteExternalLoginCommand(string? Code, string? State, string? Error, string? ErrorDescription)
    : ICommand, IRequest<Result<string>>
{
    [JsonIgnore]
    public string? Provider { get; init; }
}

public sealed class CompleteExternalLoginHandler(
    IExternalLoginRepository externalLoginRepository,
    IUserRepository userRepository,
    ISessionRepository sessionRepository,
    UserInfoFactory userInfoFactory,
    AuthenticationTokenService authenticationTokenService,
    AvatarUpdater avatarUpdater,
    ExternalAvatarClient externalAvatarClient,
    ExternalAuthenticationHelper externalAuthenticationHelper,
    ExternalAuthenticationService externalAuthenticationService,
    IHttpContextAccessor httpContextAccessor,
    IExecutionContext executionContext,
    ITelemetryEventsCollector events,
    TimeProvider timeProvider,
    ILogger<CompleteExternalLoginHandler> logger
) : IRequestHandler<CompleteExternalLoginCommand, Result<string>>
{
    public async Task<Result<string>> Handle(CompleteExternalLoginCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var validationResult = await externalAuthenticationHelper.ValidateCallback(
                command.Code, command.State, command.Error, command.ErrorDescription, ExternalLoginType.Login, cancellationToken
            );

            if (!validationResult.IsSuccess) return validationResult.ErrorResult!;

            var externalLogin = validationResult.ExternalLogin;
            var externalLoginCookie = validationResult.Cookie;
            var userProfile = validationResult.UserProfile!;

            var usersWithEmail = await userRepository.GetUsersByEmailUnfilteredAsync(userProfile.Email, cancellationToken);
            if (usersWithEmail.Length == 0)
            {
                logger.LogWarning("User not found for external login '{ExternalLoginId}'", externalLogin.Id);
                return LoginFailedRedirect(externalLogin, ExternalLoginResult.UserNotFound);
            }

            var user = externalLoginCookie.PreferredTenantId is not null
                ? usersWithEmail.SingleOrDefault(u => u.TenantId == externalLoginCookie.PreferredTenantId) ?? usersWithEmail[0]
                : usersWithEmail[0];

            var existingIdentity = user.GetExternalIdentity(externalLogin.ProviderType);
            if (existingIdentity is not null && existingIdentity.ProviderUserId != userProfile.ProviderUserId)
            {
                logger.LogWarning("Identity mismatch for user '{UserId}' with provider '{ProviderType}'", user.Id, externalLogin.ProviderType);
                return LoginFailedRedirect(externalLogin, ExternalLoginResult.IdentityMismatch);
            }

            if (existingIdentity is null)
            {
                user.AddExternalIdentity(externalLogin.ProviderType, userProfile.ProviderUserId);
                userRepository.Update(user);
            }

            if (!user.EmailConfirmed)
            {
                user.ConfirmEmail();
                userRepository.Update(user);
            }

            if (user.FirstName is null && user.LastName is null && (userProfile.FirstName is not null || userProfile.LastName is not null))
            {
                user.Update(userProfile.FirstName ?? string.Empty, userProfile.LastName ?? string.Empty, user.Title ?? string.Empty);
                userRepository.Update(user);
            }

            if (userProfile.AvatarUrl is not null && user.Avatar.Url is null)
            {
                var externalAvatar = await externalAvatarClient.DownloadAvatarAsync(userProfile.AvatarUrl, cancellationToken);
                if (externalAvatar is not null)
                {
                    await avatarUpdater.UpdateAvatar(user, false, externalAvatar.ContentType, externalAvatar.Stream, cancellationToken);
                }
            }

            externalLogin.MarkCompleted(userProfile.Email);
            externalLoginRepository.Update(externalLogin);

            var httpContext = httpContextAccessor.HttpContext!;
            var userAgent = httpContext.Request.Headers.UserAgent.ToString();
            var loginMethod = ExternalAuthenticationService.GetLoginMethod(externalLogin.ProviderType);
            var ipAddress = executionContext.ClientIpAddress;
            var session = Session.Create(user.TenantId, user.Id, loginMethod, userAgent, ipAddress);
            await sessionRepository.AddAsync(session, cancellationToken);

            user.UpdateLastSeen(timeProvider.GetUtcNow());
            userRepository.Update(user);

            var userInfo = await userInfoFactory.CreateUserInfoAsync(user, session.Id, cancellationToken);
            authenticationTokenService.CreateAndSetAuthenticationTokens(userInfo, session.Id, session.RefreshTokenJti);

            events.CollectEvent(new SessionCreated(session.Id));
            var loginTimeInSeconds = (int)(timeProvider.GetUtcNow() - externalLogin.CreatedAt).TotalSeconds;
            events.CollectEvent(new ExternalLoginCompleted(user.Id, externalLogin.ProviderType, loginTimeInSeconds));

            var returnPath = ReturnPathHelper.GetReturnPathCookie(httpContext) ?? "/";
            ReturnPathHelper.ClearReturnPathCookie(httpContext);

            return Result<string>.Redirect(returnPath);
        }
        finally
        {
            externalAuthenticationService.ClearExternalLoginCookie();
            externalAuthenticationService.ClearLocaleCookie();
        }
    }

    private Result<string> LoginFailedRedirect(ExternalLogin externalLogin, ExternalLoginResult loginResult)
    {
        var timeInSeconds = (int)(timeProvider.GetUtcNow() - externalLogin.CreatedAt).TotalSeconds;
        if (!externalLogin.IsConsumed)
        {
            externalLogin.MarkFailed(loginResult);
            externalLoginRepository.Update(externalLogin);
        }

        events.CollectEvent(new ExternalLoginFailed(externalLogin.Id, loginResult, timeInSeconds));

        var oidcError = ExternalAuthenticationService.MapToOidcError(loginResult);
        return Result<string>.Redirect($"/error?error={oidcError}&id={externalLogin.Id}");
    }
}
