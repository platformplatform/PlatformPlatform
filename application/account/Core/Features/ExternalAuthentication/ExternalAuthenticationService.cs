using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using PlatformPlatform.Account.Features.Authentication.Domain;
using PlatformPlatform.Account.Features.ExternalAuthentication.Domain;
using PlatformPlatform.Account.Integrations.OAuth;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.SinglePageApp;

namespace PlatformPlatform.Account.Features.ExternalAuthentication;

public sealed record ExternalLoginCookie(ExternalLoginId ExternalLoginId, string FingerprintHash, TenantId? PreferredTenantId);

public sealed class ExternalAuthenticationService(IHttpContextAccessor httpContextAccessor, IDataProtectionProvider dataProtectionProvider, OAuthProviderFactory oauthProviderFactory, ILogger<ExternalAuthenticationService> logger)
{
    private const string DataProtectionPurpose = "ExternalLogin";
    private const string ExternalLoginCookieName = "__Host-external-login";
    private const string LocaleCookieName = "__Host-external-login-locale";

    private static readonly string PublicUrl = Environment.GetEnvironmentVariable(SinglePageAppConfiguration.PublicUrlKey)
                                               ?? throw new InvalidOperationException($"'{SinglePageAppConfiguration.PublicUrlKey}' environment variable is not configured.");

    private readonly IDataProtector _dataProtector = dataProtectionProvider.CreateProtector(DataProtectionPurpose);

    public void SetExternalLoginCookie(ExternalLoginId externalLoginId, TenantId? preferredTenantId = null)
    {
        var fingerprintHash = GenerateBrowserFingerprintHash();
        var rawValue = preferredTenantId is not null
            ? $"{externalLoginId}|{fingerprintHash}|{preferredTenantId}"
            : $"{externalLoginId}|{fingerprintHash}";
        var cookieValue = _dataProtector.Protect(rawValue);
        httpContextAccessor.HttpContext!.Response.Cookies.Append(
            ExternalLoginCookieName,
            cookieValue,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                IsEssential = true,
                MaxAge = TimeSpan.FromSeconds(ExternalLogin.ValidForSeconds)
            }
        );
    }

    public ExternalLoginCookie? GetExternalLoginCookie()
    {
        var cookieValue = httpContextAccessor.HttpContext?.Request.Cookies[ExternalLoginCookieName];
        if (string.IsNullOrEmpty(cookieValue)) return null;

        try
        {
            var decryptedValue = _dataProtector.Unprotect(cookieValue);

            var parts = decryptedValue.Split('|');
            if (parts.Length is not (2 or 3)) return null;

            if (!ExternalLoginId.TryParse(parts[0], out var externalLoginId)) return null;

            var preferredTenantId = parts.Length == 3 && TenantId.TryParse(parts[2], out var parsedTenantId)
                ? parsedTenantId
                : null;

            return new ExternalLoginCookie(externalLoginId, parts[1], preferredTenantId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to decrypt external login cookie");
            return null;
        }
    }

    public void ClearExternalLoginCookie()
    {
        httpContextAccessor.HttpContext!.Response.Cookies.Delete(ExternalLoginCookieName, new CookieOptions { Secure = true });
    }

    public void SetLocaleCookie(string locale)
    {
        httpContextAccessor.HttpContext!.Response.Cookies.Append(
            LocaleCookieName,
            locale,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                IsEssential = true,
                MaxAge = TimeSpan.FromSeconds(ExternalLogin.ValidForSeconds)
            }
        );
    }

    public string? GetLocaleCookie()
    {
        return httpContextAccessor.HttpContext?.Request.Cookies[LocaleCookieName];
    }

    public void ClearLocaleCookie()
    {
        httpContextAccessor.HttpContext!.Response.Cookies.Delete(LocaleCookieName, new CookieOptions { Secure = true });
    }

    /// <summary>
    ///     Validates that the current browser fingerprint matches the one stored at login initiation.
    ///     This is a best-effort defense-in-depth measure, not a security boundary. It may detect
    ///     opportunistic session hijacking but will not stop determined attackers who can replay
    ///     the victim's User-Agent and Accept-Language headers. Primary security relies on PKCE,
    ///     nonce validation, state token encryption, and cookie binding.
    /// </summary>
    public bool ValidateBrowserFingerprint(string fingerprintHash)
    {
        if (oauthProviderFactory.ShouldUseMockProvider(httpContextAccessor.HttpContext!))
        {
            return true;
        }

        return GenerateBrowserFingerprintHash() == fingerprintHash;
    }

    /// <summary>
    ///     Generates a SHA-256 hash of User-Agent and Accept-Language headers as a low-entropy
    ///     browser fingerprint. Known limitations: Chrome UA reduction means many browsers share
    ///     identical frozen UA strings, headers are trivially spoofable by an attacker who
    ///     intercepts the OAuth authorization code, and many users share identical combinations.
    ///     This serves as forensic/telemetry signal rather than an authorization boundary.
    /// </summary>
    public string GenerateBrowserFingerprintHash()
    {
        var httpContext = httpContextAccessor.HttpContext!;
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();
        var acceptLanguage = httpContext.Request.Headers.AcceptLanguage.ToString();
        var fingerprint = $"{userAgent}|{acceptLanguage}";
        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(fingerprint)));
    }

    public string ProtectState(ExternalLoginId externalLoginId)
    {
        return _dataProtector.Protect(externalLoginId.ToString());
    }

    public ExternalLoginId? GetExternalLoginIdFromState(string? state)
    {
        if (string.IsNullOrEmpty(state)) return null;

        try
        {
            var decryptedState = _dataProtector.Unprotect(state);
            return new ExternalLoginId(decryptedState);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to decrypt state");
            return null;
        }
    }

    public static string GetRedirectUri(ExternalProviderType providerType, ExternalLoginType loginType)
    {
        var loginTypeSegment = loginType == ExternalLoginType.Login ? "login" : "signup";
        return $"{PublicUrl}/api/account/authentication/{providerType}/{loginTypeSegment}/callback";
    }

    public static LoginMethod GetLoginMethod(ExternalProviderType providerType)
    {
        return providerType switch
        {
            ExternalProviderType.Google => LoginMethod.Google,
            _ => throw new UnreachableException()
        };
    }

    public static string MapToOidcError(ExternalLoginResult internalError)
    {
        return internalError switch
        {
            ExternalLoginResult.SessionHijackingDetected => "authentication_failed",
            ExternalLoginResult.FlowIdMismatch => "authentication_failed",
            ExternalLoginResult.LoginReplayDetected => "authentication_failed",
            ExternalLoginResult.LoginAlreadyCompleted => "authentication_failed",
            ExternalLoginResult.InvalidState => "invalid_request",
            ExternalLoginResult.CodeExchangeFailed => "authentication_failed",
            ExternalLoginResult.NonceMismatch => "authentication_failed",
            ExternalLoginResult.SessionNotFound => "session_expired",
            ExternalLoginResult.LoginExpired => "session_expired",
            ExternalLoginResult.IdentityMismatch => "authentication_failed",
            ExternalLoginResult.IdentityProviderError => "authentication_failed",
            ExternalLoginResult.UserNotFound => "user_not_found",
            ExternalLoginResult.AccountAlreadyExists => "account_already_exists",
            _ => "server_error"
        };
    }
}
