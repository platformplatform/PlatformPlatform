using System.Security.Cryptography;
using System.Text;
using JetBrains.Annotations;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using PlatformPlatform.Account.Features.Authentication.Domain;
using PlatformPlatform.Account.Features.ExternalAuthentication.Domain;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.Account.Features.Users.Shared;
using PlatformPlatform.Account.Integrations.Gravatar;
using PlatformPlatform.Account.Integrations.OAuth;
using PlatformPlatform.SharedKernel.Authentication.TokenGeneration;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.OpenIdConnect;
using PlatformPlatform.SharedKernel.SinglePageApp;
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
    OAuthProviderFactory oauthProviderFactory,
    UserInfoFactory userInfoFactory,
    AuthenticationTokenService authenticationTokenService,
    AvatarUpdater avatarUpdater,
    GravatarClient gravatarClient,
    IHttpContextAccessor httpContextAccessor,
    IExecutionContext executionContext,
    IDataProtectionProvider dataProtectionProvider,
    ITelemetryEventsCollector events,
    TimeProvider timeProvider,
    ILogger<CompleteExternalLoginHandler> logger
) : IRequestHandler<CompleteExternalLoginCommand, Result<string>>
{
    private const string DataProtectionPurpose = "ExternalLogin";
    private const string ExternalLoginCookieName = "__Host_External_Login";

    private static readonly string PublicUrl = Environment.GetEnvironmentVariable(SinglePageAppConfiguration.PublicUrlKey)
                                               ?? throw new InvalidOperationException($"'{SinglePageAppConfiguration.PublicUrlKey}' environment variable is not configured.");

    public async Task<Result<string>> Handle(CompleteExternalLoginCommand command, CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext!;

        try
        {
            var externalLoginCookie = GetExternalLoginCookie(httpContext);
            var externalLoginIdFromState = GetExternalLoginIdFromState(command.State);

            if (externalLoginIdFromState is null && externalLoginCookie is null)
            {
                logger.LogWarning("Missing state and cookie");
                return LoginFailedRedirect(null, ExternalLoginResult.InvalidState);
            }

            Activity.Current?.SetTag("flow_id", externalLoginIdFromState?.ToString() ?? externalLoginCookie?.ExternalLoginId.ToString());

            if (externalLoginIdFromState is null)
            {
                logger.LogWarning("Missing external login ID from state");
                return LoginFailedRedirect(null, ExternalLoginResult.InvalidState);
            }

            if (externalLoginCookie is null)
            {
                logger.LogWarning("Replay detected for flow {FlowId} - session cookie missing", externalLoginIdFromState);
                return LoginFailedRedirect(null, ExternalLoginResult.LoginReplayDetected);
            }

            var externalLogin = await externalLoginRepository.GetByIdAsync(externalLoginIdFromState, cancellationToken);
            if (externalLogin is null)
            {
                logger.LogWarning("Session not found for external login {ExternalLoginId}", externalLoginIdFromState);
                return LoginFailedRedirect(null, ExternalLoginResult.SessionNotFound);
            }

            if (externalLoginIdFromState != externalLoginCookie.ExternalLoginId)
            {
                logger.LogWarning("Flow ID mismatch for external login {ExternalLoginId}", externalLoginIdFromState);
                return LoginFailedRedirect(externalLogin, ExternalLoginResult.FlowIdMismatch);
            }

            var userAgent = httpContext.Request.Headers.UserAgent.ToString();
            var useMockProvider = oauthProviderFactory.ShouldUseMockProvider(httpContext);
            if (!useMockProvider)
            {
                var acceptLanguage = httpContext.Request.Headers.AcceptLanguage.ToString();
                var currentFingerprint = ComputeBrowserFingerprint(userAgent, acceptLanguage);

                if (currentFingerprint != externalLoginCookie.FingerprintHash)
                {
                    logger.LogWarning("Session hijacking detected for external login '{ExternalLoginId}'", externalLoginIdFromState);
                    return LoginFailedRedirect(externalLogin, ExternalLoginResult.SessionHijackingDetected);
                }
            }

            if (externalLogin.IsExpired(timeProvider.GetUtcNow()))
            {
                logger.LogWarning("Login expired for external login {ExternalLoginId}", externalLogin.Id);
                return LoginFailedRedirect(externalLogin, ExternalLoginResult.LoginExpired);
            }

            if (externalLogin.IsConsumed)
            {
                logger.LogWarning("Login already completed for external login {ExternalLoginId}", externalLoginIdFromState);
                return LoginFailedRedirect(externalLogin, ExternalLoginResult.LoginAlreadyCompleted);
            }

            if (!string.IsNullOrEmpty(command.Error))
            {
                logger.LogWarning("OAuth error received: {Error} - {ErrorDescription}", command.Error, command.ErrorDescription);
                return OAuthErrorRedirect(externalLogin, command.Error);
            }

            if (string.IsNullOrEmpty(command.Code))
            {
                logger.LogWarning("Authorization code missing from OAuth callback");
                return LoginFailedRedirect(externalLogin, ExternalLoginResult.CodeExchangeFailed);
            }

            var oauthProvider = oauthProviderFactory.GetProvider(externalLogin.ProviderType, useMockProvider);
            if (oauthProvider is null)
            {
                logger.LogWarning("Provider {ProviderType} not configured", externalLogin.ProviderType);
                return LoginFailedRedirect(externalLogin, ExternalLoginResult.CodeExchangeFailed);
            }

            var redirectUri = GetRedirectUri(externalLogin.ProviderType);
            var tokenResponse = await oauthProvider.ExchangeCodeForTokensAsync(command.Code, externalLogin.CodeVerifier, redirectUri, cancellationToken);
            if (tokenResponse is null)
            {
                logger.LogWarning("Token exchange failed for external login {ExternalLoginId}", externalLogin.Id);
                return LoginFailedRedirect(externalLogin, ExternalLoginResult.CodeExchangeFailed);
            }

            var userProfile = oauthProvider.GetUserProfile(tokenResponse);
            if (userProfile is null)
            {
                logger.LogWarning("Failed to get user profile for external login {ExternalLoginId}", externalLogin.Id);
                return LoginFailedRedirect(externalLogin, ExternalLoginResult.CodeExchangeFailed);
            }

            if (!userProfile.EmailVerified)
            {
                logger.LogWarning("Email not verified for external login {ExternalLoginId}", externalLogin.Id);
                return LoginFailedRedirect(externalLogin, ExternalLoginResult.CodeExchangeFailed);
            }

            var user = await userRepository.GetUserByEmailUnfilteredAsync(userProfile.Email, cancellationToken);
            if (user is null)
            {
                logger.LogWarning("User not found for email {Email}", userProfile.Email);
                externalLogin.MarkFailed(ExternalLoginResult.UserNotFound);
                externalLoginRepository.Update(externalLogin);
                var timeInSeconds = (int)(timeProvider.GetUtcNow() - externalLogin.CreatedAt).TotalSeconds;
                events.CollectEvent(new ExternalLoginFailed(externalLogin.Id, ExternalLoginResult.UserNotFound, timeInSeconds));
                return Result<string>.Redirect($"/error?error=user_not_found&id={externalLogin.Id}");
            }

            var existingIdentity = user.GetExternalIdentity(externalLogin.ProviderType);
            if (existingIdentity is not null && existingIdentity.ProviderUserId != userProfile.ProviderUserId)
            {
                logger.LogWarning("Identity mismatch for user {UserId} with provider {ProviderType}", user.Id, externalLogin.ProviderType);
                externalLogin.MarkFailed(ExternalLoginResult.IdentityMismatch);
                externalLoginRepository.Update(externalLogin);
                var timeInSeconds = (int)(timeProvider.GetUtcNow() - externalLogin.CreatedAt).TotalSeconds;
                events.CollectEvent(new ExternalLoginFailed(externalLogin.Id, ExternalLoginResult.IdentityMismatch, timeInSeconds));
                return Result<string>.Redirect($"/error?error=identity_mismatch&id={externalLogin.Id}");
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

            externalLogin.MarkCompleted();
            externalLoginRepository.Update(externalLogin);

            var loginMethod = GetLoginMethod(externalLogin.ProviderType);
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
            ClearExternalLoginCookie(httpContext);
        }
    }

    private Result<string> LoginFailedRedirect(ExternalLogin? externalLogin, ExternalLoginResult loginResult)
    {
        var timeInSeconds = 0;

        if (externalLogin is not null)
        {
            timeInSeconds = (int)(timeProvider.GetUtcNow() - externalLogin.CreatedAt).TotalSeconds;
            if (!externalLogin.IsConsumed)
            {
                externalLogin.MarkFailed(loginResult);
                externalLoginRepository.Update(externalLogin);
            }
        }

        events.CollectEvent(new ExternalLoginFailed(externalLogin?.Id, loginResult, timeInSeconds));

        var oidcError = MapToOidcError(loginResult);
        var referenceId = externalLogin?.Id.ToString() ?? Activity.Current?.TraceId.ToString();
        var redirectUrl = $"/error?error={oidcError}&id={referenceId}";

        return Result<string>.Redirect(redirectUrl);
    }

    private Result<string> OAuthErrorRedirect(ExternalLogin externalLogin, string oauthError)
    {
        var timeInSeconds = (int)(timeProvider.GetUtcNow() - externalLogin.CreatedAt).TotalSeconds;
        if (!externalLogin.IsConsumed)
        {
            externalLogin.MarkFailed(ExternalLoginResult.IdentityProviderError);
            externalLoginRepository.Update(externalLogin);
        }

        events.CollectEvent(new ExternalLoginFailed(externalLogin.Id, ExternalLoginResult.IdentityProviderError, timeInSeconds));

        return Result<string>.Redirect($"/error?error={oauthError}&id={externalLogin.Id}");
    }

    private ExternalLoginId? GetExternalLoginIdFromState(string? state)
    {
        if (string.IsNullOrEmpty(state)) return null;

        try
        {
            var dataProtector = dataProtectionProvider.CreateProtector(DataProtectionPurpose);
            var decryptedState = dataProtector.Unprotect(state);
            return new ExternalLoginId(decryptedState);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to decrypt state");
            return null;
        }
    }

    private static ExternalLoginCookie? GetExternalLoginCookie(HttpContext httpContext)
    {
        var cookieValue = httpContext.Request.Cookies[ExternalLoginCookieName];
        if (string.IsNullOrEmpty(cookieValue)) return null;

        var parts = cookieValue.Split('|');
        if (parts.Length != 2) return null;

        return new ExternalLoginCookie(new ExternalLoginId(parts[0]), parts[1]);
    }

    private static void ClearExternalLoginCookie(HttpContext httpContext)
    {
        httpContext.Response.Cookies.Delete(ExternalLoginCookieName);
    }

    private static string ComputeBrowserFingerprint(string userAgent, string acceptLanguage)
    {
        var fingerprintSource = $"{userAgent}|{acceptLanguage}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(fingerprintSource));
        return Convert.ToBase64String(hash);
    }

    private static string GetRedirectUri(ExternalProviderType providerType)
    {
        return $"{PublicUrl}/api/account/authentication/{providerType}/login/callback";
    }

    private static LoginMethod GetLoginMethod(ExternalProviderType providerType)
    {
        return providerType switch
        {
            ExternalProviderType.Google => LoginMethod.Google,
            _ => throw new UnreachableException()
        };
    }

    private static string MapToOidcError(ExternalLoginResult internalError)
    {
        return internalError switch
        {
            ExternalLoginResult.SessionHijackingDetected => "authentication_failed",
            ExternalLoginResult.FlowIdMismatch => "authentication_failed",
            ExternalLoginResult.LoginReplayDetected => "authentication_failed",
            ExternalLoginResult.LoginAlreadyCompleted => "authentication_failed",
            ExternalLoginResult.InvalidState => "invalid_request",
            ExternalLoginResult.CodeExchangeFailed => "authentication_failed",
            ExternalLoginResult.SessionNotFound => "session_expired",
            ExternalLoginResult.LoginExpired => "session_expired",
            ExternalLoginResult.IdentityMismatch => "authentication_failed",
            ExternalLoginResult.IdentityProviderError => "authentication_failed",
            ExternalLoginResult.UserNotFound => "user_not_found",
            _ => "server_error"
        };
    }

    private sealed record ExternalLoginCookie(ExternalLoginId ExternalLoginId, string FingerprintHash);
}
