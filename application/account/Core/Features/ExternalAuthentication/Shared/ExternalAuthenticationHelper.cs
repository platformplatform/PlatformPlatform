using Microsoft.AspNetCore.Http;
using PlatformPlatform.Account.Features.ExternalAuthentication.Domain;
using PlatformPlatform.Account.Integrations.OAuth;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.Account.Features.ExternalAuthentication.Shared;

internal sealed record CallbackValidationResult(
    bool IsSuccess,
    ExternalLogin ExternalLogin,
    ExternalLoginCookie Cookie,
    OAuthUserProfile? UserProfile,
    Result<string>? ErrorResult
)
{
    public static CallbackValidationResult Success(ExternalLogin externalLogin, ExternalLoginCookie cookie, OAuthUserProfile userProfile)
    {
        return new CallbackValidationResult(true, externalLogin, cookie, userProfile, null);
    }

    public static CallbackValidationResult Failure(ExternalLogin externalLogin, ExternalLoginCookie cookie, Result<string> errorResult)
    {
        return new CallbackValidationResult(false, externalLogin, cookie, null, errorResult);
    }
}

public sealed class ExternalAuthenticationHelper(
    IExternalLoginRepository externalLoginRepository,
    OAuthProviderFactory oauthProviderFactory,
    ExternalAuthenticationService externalAuthenticationService,
    IHttpContextAccessor httpContextAccessor,
    ITelemetryEventsCollector events,
    TimeProvider timeProvider,
    ILogger<ExternalAuthenticationHelper> logger
)
{
    internal async Task<CallbackValidationResult> ValidateCallback(
        string? code,
        string? state,
        string? error,
        string? errorDescription,
        ExternalLoginType loginType,
        CancellationToken cancellationToken
    )
    {
        var externalLoginCookie = externalAuthenticationService.GetExternalLoginCookie();
        var externalLoginIdFromState = externalAuthenticationService.GetExternalLoginIdFromState(state);

        if (externalLoginIdFromState is null && externalLoginCookie is null)
        {
            logger.LogWarning("Missing state and cookie");
            return FailedRedirect(null!, externalLoginCookie!, ExternalLoginResult.InvalidState, loginType);
        }

        Activity.Current?.SetTag("flow_id", externalLoginIdFromState?.ToString() ?? externalLoginCookie?.ExternalLoginId.ToString());

        if (externalLoginIdFromState is null)
        {
            logger.LogWarning("Missing external login ID from state");
            return FailedRedirect(null!, externalLoginCookie!, ExternalLoginResult.InvalidState, loginType);
        }

        if (externalLoginCookie is null)
        {
            logger.LogWarning("Replay detected for flow '{FlowId}' - session cookie missing", externalLoginIdFromState);
            return FailedRedirect(null!, externalLoginCookie!, ExternalLoginResult.LoginReplayDetected, loginType);
        }

        var externalLogin = await externalLoginRepository.GetByIdAsync(externalLoginIdFromState, cancellationToken);
        if (externalLogin is null)
        {
            logger.LogWarning("Session not found for external login '{ExternalLoginId}'", externalLoginIdFromState);
            return FailedRedirect(null!, externalLoginCookie, ExternalLoginResult.SessionNotFound, loginType);
        }

        if (externalLoginIdFromState != externalLoginCookie.ExternalLoginId)
        {
            logger.LogWarning("Flow ID mismatch for external login '{ExternalLoginId}'", externalLoginIdFromState);
            return FailedRedirect(externalLogin, externalLoginCookie, ExternalLoginResult.FlowIdMismatch, loginType);
        }

        if (!externalAuthenticationService.ValidateBrowserFingerprint(externalLoginCookie.FingerprintHash))
        {
            logger.LogWarning("Session hijacking detected for external login '{ExternalLoginId}'", externalLoginIdFromState);
            return FailedRedirect(externalLogin, externalLoginCookie, ExternalLoginResult.SessionHijackingDetected, loginType);
        }

        if (externalLogin.IsExpired(timeProvider.GetUtcNow()))
        {
            logger.LogWarning("Login expired for external login '{ExternalLoginId}'", externalLogin.Id);
            return FailedRedirect(externalLogin, externalLoginCookie, ExternalLoginResult.LoginExpired, loginType);
        }

        if (externalLogin.IsConsumed)
        {
            logger.LogWarning("Login already completed for external login '{ExternalLoginId}'", externalLoginIdFromState);
            return FailedRedirect(externalLogin, externalLoginCookie, ExternalLoginResult.LoginAlreadyCompleted, loginType);
        }

        if (!string.IsNullOrEmpty(error))
        {
            logger.LogWarning("OAuth error received: '{Error}' - '{ErrorDescription}'", error, errorDescription);
            return OAuthErrorRedirect(externalLogin, externalLoginCookie, error, loginType);
        }

        if (string.IsNullOrEmpty(code))
        {
            logger.LogWarning("Authorization code missing from OAuth callback");
            return FailedRedirect(externalLogin, externalLoginCookie, ExternalLoginResult.CodeExchangeFailed, loginType);
        }

        var httpContext = httpContextAccessor.HttpContext!;
        var useMockProvider = oauthProviderFactory.ShouldUseMockProvider(httpContext);
        var oauthProvider = oauthProviderFactory.GetProvider(externalLogin.ProviderType, useMockProvider);
        if (oauthProvider is null)
        {
            logger.LogWarning("Provider '{ProviderType}' not configured", externalLogin.ProviderType);
            return FailedRedirect(externalLogin, externalLoginCookie, ExternalLoginResult.CodeExchangeFailed, loginType);
        }

        var redirectUri = ExternalAuthenticationService.GetRedirectUri(externalLogin.ProviderType, loginType);
        var tokenResponse = await oauthProvider.ExchangeCodeForTokensAsync(code, externalLogin.CodeVerifier, redirectUri, cancellationToken);
        if (tokenResponse is null)
        {
            logger.LogWarning("Token exchange failed for external login '{ExternalLoginId}'", externalLogin.Id);
            return FailedRedirect(externalLogin, externalLoginCookie, ExternalLoginResult.CodeExchangeFailed, loginType);
        }

        var userProfile = await oauthProvider.GetUserProfileAsync(tokenResponse, cancellationToken);
        if (userProfile is null)
        {
            logger.LogWarning("Failed to get user profile for external login '{ExternalLoginId}'", externalLogin.Id);
            return FailedRedirect(externalLogin, externalLoginCookie, ExternalLoginResult.CodeExchangeFailed, loginType);
        }

        if (!userProfile.EmailVerified)
        {
            logger.LogWarning("Email not verified for external login '{ExternalLoginId}'", externalLogin.Id);
            return FailedRedirect(externalLogin, externalLoginCookie, ExternalLoginResult.CodeExchangeFailed, loginType);
        }

        if (userProfile.Nonce != externalLogin.Nonce)
        {
            logger.LogWarning("Nonce mismatch for external login '{ExternalLoginId}'", externalLogin.Id);
            return FailedRedirect(externalLogin, externalLoginCookie, ExternalLoginResult.NonceMismatch, loginType);
        }

        return CallbackValidationResult.Success(externalLogin, externalLoginCookie, userProfile);
    }

    private CallbackValidationResult FailedRedirect(
        ExternalLogin? externalLogin,
        ExternalLoginCookie cookie,
        ExternalLoginResult loginResult,
        ExternalLoginType loginType
    )
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

        CollectFailedEvent(loginType, externalLogin, loginResult, timeInSeconds, null);

        var oidcError = ExternalAuthenticationService.MapToOidcError(loginResult);
        var referenceId = externalLogin?.Id.ToString() ?? Activity.Current?.TraceId.ToString();
        var redirectUrl = $"/error?error={oidcError}&id={referenceId}";

        var errorResult = Result<string>.Redirect(redirectUrl);
        return CallbackValidationResult.Failure(externalLogin!, cookie, errorResult);
    }

    private CallbackValidationResult OAuthErrorRedirect(
        ExternalLogin externalLogin,
        ExternalLoginCookie cookie,
        string oauthError,
        ExternalLoginType loginType
    )
    {
        var timeInSeconds = (int)(timeProvider.GetUtcNow() - externalLogin.CreatedAt).TotalSeconds;
        if (!externalLogin.IsConsumed)
        {
            externalLogin.MarkFailed(ExternalLoginResult.IdentityProviderError);
            externalLoginRepository.Update(externalLogin);
        }

        CollectFailedEvent(loginType, externalLogin, ExternalLoginResult.IdentityProviderError, timeInSeconds, oauthError);

        var sanitizedError = Uri.EscapeDataString(oauthError);
        var errorResult = Result<string>.Redirect($"/error?error={sanitizedError}&id={externalLogin.Id}");
        return CallbackValidationResult.Failure(externalLogin, cookie, errorResult);
    }

    private void CollectFailedEvent(ExternalLoginType loginType, ExternalLogin? externalLogin, ExternalLoginResult loginResult, int timeInSeconds, string? oauthError)
    {
        if (loginType == ExternalLoginType.Login)
        {
            events.CollectEvent(new ExternalLoginFailed(externalLogin?.Id, loginResult, timeInSeconds, oauthError));
        }
        else
        {
            events.CollectEvent(new ExternalSignupFailed(externalLogin?.Id, loginResult, timeInSeconds, oauthError));
        }
    }
}
