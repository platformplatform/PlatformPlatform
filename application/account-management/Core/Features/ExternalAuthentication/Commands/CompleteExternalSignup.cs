using System.Security.Cryptography;
using System.Text;
using JetBrains.Annotations;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using PlatformPlatform.AccountManagement.Features.Authentication.Domain;
using PlatformPlatform.AccountManagement.Features.ExternalAuthentication.Domain;
using PlatformPlatform.AccountManagement.Features.Tenants.Commands;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Shared;
using PlatformPlatform.AccountManagement.Integrations.OAuth;
using PlatformPlatform.SharedKernel.Authentication.TokenGeneration;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.OpenIdConnect;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.AccountManagement.Features.ExternalAuthentication.Commands;

[PublicAPI]
public sealed record CompleteExternalSignupCommand(string? Code, string? State, string? Error, string? ErrorDescription)
    : ICommand, IRequest<Result<string>>
{
    [JsonIgnore]
    public string? Provider { get; init; }
}

public sealed class CompleteExternalSignupHandler(
    IExternalLoginRepository externalLoginRepository,
    IUserRepository userRepository,
    ISessionRepository sessionRepository,
    OAuthProviderFactory oauthProviderFactory,
    UserInfoFactory userInfoFactory,
    AuthenticationTokenService authenticationTokenService,
    IHttpContextAccessor httpContextAccessor,
    IExecutionContext executionContext,
    IDataProtectionProvider dataProtectionProvider,
    IMediator mediator,
    ITelemetryEventsCollector events,
    TimeProvider timeProvider,
    ILogger<CompleteExternalSignupHandler> logger
) : IRequestHandler<CompleteExternalSignupCommand, Result<string>>
{
    private const string DataProtectionPurpose = "ExternalLogin";
    private const string ExternalLoginCookieName = "__Host_External_Login";

    public async Task<Result<string>> Handle(CompleteExternalSignupCommand command, CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext!;

        try
        {
            var externalLoginCookie = GetExternalLoginCookie(httpContext);
            var externalLoginIdFromState = GetExternalLoginIdFromState(command.State);

            if (externalLoginIdFromState is null && externalLoginCookie is null)
            {
                logger.LogWarning("Missing state and cookie");
                return SignupFailedRedirect(null, ExternalLoginResult.InvalidState);
            }

            Activity.Current?.SetTag("flow_id", externalLoginIdFromState?.ToString() ?? externalLoginCookie?.ExternalLoginId.ToString());

            if (externalLoginIdFromState is null)
            {
                logger.LogWarning("Missing external login ID from state");
                return SignupFailedRedirect(null, ExternalLoginResult.InvalidState);
            }

            if (externalLoginCookie is null)
            {
                logger.LogWarning("Replay detected for flow {FlowId} - session cookie missing", externalLoginIdFromState);
                return SignupFailedRedirect(null, ExternalLoginResult.LoginReplayDetected);
            }

            var externalLogin = await externalLoginRepository.GetByIdAsync(externalLoginIdFromState, cancellationToken);
            if (externalLogin is null)
            {
                logger.LogWarning("Session not found for external login {ExternalLoginId}", externalLoginIdFromState);
                return SignupFailedRedirect(null, ExternalLoginResult.SessionNotFound);
            }

            if (externalLoginIdFromState != externalLoginCookie.ExternalLoginId)
            {
                logger.LogWarning("Flow ID mismatch for external login {ExternalLoginId}", externalLoginIdFromState);
                return SignupFailedRedirect(externalLogin, ExternalLoginResult.FlowIdMismatch);
            }

            var userAgent = httpContext.Request.Headers.UserAgent.ToString();
            var acceptLanguage = httpContext.Request.Headers.AcceptLanguage.ToString();
            var currentFingerprint = ComputeBrowserFingerprint(userAgent, acceptLanguage);
            if (currentFingerprint != externalLoginCookie.FingerprintHash || currentFingerprint != externalLogin.BrowserFingerprint)
            {
                logger.LogWarning("Session hijacking detected for external login {ExternalLoginId}", externalLoginIdFromState);
                return SignupFailedRedirect(externalLogin, ExternalLoginResult.SessionHijackingDetected);
            }

            if (externalLogin.IsExpired(timeProvider.GetUtcNow()))
            {
                logger.LogWarning("Login expired for external login {ExternalLoginId}", externalLogin.Id);
                return SignupFailedRedirect(externalLogin, ExternalLoginResult.LoginExpired);
            }

            if (externalLogin.IsConsumed)
            {
                logger.LogWarning("Login already completed for external login {ExternalLoginId}", externalLoginIdFromState);
                return SignupFailedRedirect(externalLogin, ExternalLoginResult.LoginAlreadyCompleted);
            }

            if (!string.IsNullOrEmpty(command.Error))
            {
                logger.LogWarning("OAuth error received: {Error} - {ErrorDescription}", command.Error, command.ErrorDescription);
                return OAuthErrorRedirect(externalLogin, command.Error);
            }

            if (string.IsNullOrEmpty(command.Code))
            {
                logger.LogWarning("Authorization code missing from OAuth callback");
                return SignupFailedRedirect(externalLogin, ExternalLoginResult.CodeExchangeFailed);
            }

            var useMockProvider = oauthProviderFactory.ShouldUseMockProvider(httpContext);
            var oauthProvider = oauthProviderFactory.GetProvider(externalLogin.ProviderType, useMockProvider);
            if (oauthProvider is null)
            {
                logger.LogWarning("Provider {ProviderType} not configured", externalLogin.ProviderType);
                return SignupFailedRedirect(externalLogin, ExternalLoginResult.CodeExchangeFailed);
            }

            var redirectUri = GetRedirectUri(httpContext, externalLogin.ProviderType);
            var tokenResponse = await oauthProvider.ExchangeCodeForTokensAsync(command.Code, externalLogin.CodeVerifier, redirectUri, cancellationToken);
            if (tokenResponse is null)
            {
                logger.LogWarning("Token exchange failed for external login {ExternalLoginId}", externalLogin.Id);
                return SignupFailedRedirect(externalLogin, ExternalLoginResult.CodeExchangeFailed);
            }

            var userProfile = oauthProvider.GetUserProfile(tokenResponse);
            if (userProfile is null)
            {
                logger.LogWarning("Failed to get user profile for external login {ExternalLoginId}", externalLogin.Id);
                return SignupFailedRedirect(externalLogin, ExternalLoginResult.CodeExchangeFailed);
            }

            if (!userProfile.EmailVerified)
            {
                logger.LogWarning("Email not verified for external login {ExternalLoginId}", externalLogin.Id);
                return SignupFailedRedirect(externalLogin, ExternalLoginResult.CodeExchangeFailed);
            }

            var existingUser = await userRepository.GetUserByEmailUnfilteredAsync(userProfile.Email, cancellationToken);
            if (existingUser is not null)
            {
                logger.LogWarning("User already exists for email {Email}", userProfile.Email);
                return Result<string>.Redirect("/login?email=" + Uri.EscapeDataString(userProfile.Email));
            }

            var locale = externalLogin.Locale ?? userProfile.Locale;

            var createTenantResult = await mediator.Send(new CreateTenantCommand(userProfile.Email, true, locale), cancellationToken);
            if (!createTenantResult.IsSuccess)
            {
                logger.LogWarning("Failed to create tenant for external signup {ExternalLoginId}", externalLogin.Id);
                return SignupFailedRedirect(externalLogin, ExternalLoginResult.CodeExchangeFailed);
            }

            var user = await userRepository.GetByIdAsync(createTenantResult.Value!.UserId, cancellationToken);
            if (user is null)
            {
                logger.LogWarning("Failed to get user after tenant creation for external signup {ExternalLoginId}", externalLogin.Id);
                return SignupFailedRedirect(externalLogin, ExternalLoginResult.CodeExchangeFailed);
            }

            user.AddExternalIdentity(externalLogin.ProviderType, userProfile.ProviderUserId);

            if (userProfile.FirstName is not null || userProfile.LastName is not null)
            {
                user.Update(userProfile.FirstName ?? string.Empty, userProfile.LastName ?? string.Empty, string.Empty);
            }

            userRepository.Update(user);

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
            var signupTimeInSeconds = (int)(timeProvider.GetUtcNow() - externalLogin.CreatedAt).TotalSeconds;
            events.CollectEvent(new ExternalSignupCompleted(createTenantResult.Value.TenantId, externalLogin.ProviderType, signupTimeInSeconds));

            var returnPath = ReturnPathHelper.GetReturnPathCookie(httpContext) ?? "/";
            ReturnPathHelper.ClearReturnPathCookie(httpContext);

            return Result<string>.Redirect(returnPath);
        }
        finally
        {
            ClearExternalLoginCookie(httpContext);
        }
    }

    private Result<string> SignupFailedRedirect(ExternalLogin? externalLogin, ExternalLoginResult loginResult)
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

        events.CollectEvent(new ExternalSignupFailed(externalLogin?.Id, loginResult, timeInSeconds));

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

        events.CollectEvent(new ExternalSignupFailed(externalLogin.Id, ExternalLoginResult.IdentityProviderError, timeInSeconds));

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

    private static string GetRedirectUri(HttpContext httpContext, ExternalProviderType providerType)
    {
        var scheme = httpContext.Request.Scheme;
        var host = httpContext.Request.Host;
        return $"{scheme}://{host}/api/account-management/authentication/{providerType}/signup/callback";
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
