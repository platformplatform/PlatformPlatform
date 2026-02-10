using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using PlatformPlatform.AccountManagement.Features.ExternalAuthentication.Domain;

namespace PlatformPlatform.AccountManagement.Integrations.OAuth.Mock;

public sealed class MockOAuthProvider(IConfiguration configuration, IHttpContextAccessor httpContextAccessor) : IOAuthProvider
{
    public const string MockEmail = $"mockuser{OAuthProviderFactory.MockEmailDomain}";
    public const string MockProviderUserId = "mock-google-user-id-12345";
    public const string MockFirstName = "Mock";
    public const string MockLastName = "User";
    public const string FailurePrefix = "fail:";

    private readonly bool _isEnabled = configuration.GetValue<bool>("OAuth:AllowMockProvider");

    public ExternalProviderType ProviderType => ExternalProviderType.Google;

    public string BuildAuthorizationUrl(string stateToken, string codeChallenge, string nonce, string redirectUri)
    {
        if (!_isEnabled)
        {
            throw new InvalidOperationException("Mock OAuth provider is not enabled.");
        }

        var failureMode = GetFailureMode(httpContextAccessor.HttpContext!);
        if (failureMode == "access_denied")
        {
            return $"{redirectUri}?error=access_denied&error_description=The+user+denied+access&state={Uri.EscapeDataString(stateToken)}";
        }

        return $"{redirectUri}?code=mock-authorization-code:{Uri.EscapeDataString(nonce)}&state={Uri.EscapeDataString(stateToken)}";
    }

    public Task<OAuthTokenResponse?> ExchangeCodeForTokensAsync(string code, string codeVerifier, string redirectUri, CancellationToken cancellationToken)
    {
        if (!_isEnabled)
        {
            throw new InvalidOperationException("Mock OAuth provider is not enabled.");
        }

        var failureMode = GetFailureMode(httpContextAccessor.HttpContext!);
        if (failureMode == "token_exchange")
        {
            return Task.FromResult<OAuthTokenResponse?>(null);
        }

        var nonce = ExtractNonceFromMockCode(code);
        var mockTokenResponse = new OAuthTokenResponse(
            "mock-access-token",
            $"mock-id-token:{nonce}",
            3600
        );

        return Task.FromResult<OAuthTokenResponse?>(mockTokenResponse);
    }

    public Task<OAuthUserProfile?> GetUserProfileAsync(OAuthTokenResponse tokenResponse, CancellationToken cancellationToken)
    {
        if (!_isEnabled)
        {
            throw new InvalidOperationException("Mock OAuth provider is not enabled.");
        }

        var failureMode = GetFailureMode(httpContextAccessor.HttpContext!);
        var emailPrefix = GetMockEmailPrefix(httpContextAccessor.HttpContext!);
        var email = emailPrefix is not null ? $"{emailPrefix}{OAuthProviderFactory.MockEmailDomain}" : MockEmail;
        var providerUserId = emailPrefix is not null ? $"mock-google-{emailPrefix}" : MockProviderUserId;
        var emailVerified = failureMode != "email_not_verified";
        var nonce = ExtractNonceFromMockIdToken(tokenResponse.IdToken);

        return Task.FromResult<OAuthUserProfile?>(new OAuthUserProfile(
                providerUserId,
                email,
                emailVerified,
                MockFirstName,
                MockLastName,
                null,
                "en",
                nonce
            )
        );
    }

    private static string? ExtractNonceFromMockCode(string code)
    {
        var separatorIndex = code.IndexOf(':');
        return separatorIndex >= 0 ? Uri.UnescapeDataString(code[(separatorIndex + 1)..]) : null;
    }

    private static string? ExtractNonceFromMockIdToken(string? idToken)
    {
        if (idToken is null) return null;
        var separatorIndex = idToken.IndexOf(':');
        return separatorIndex >= 0 ? idToken[(separatorIndex + 1)..] : null;
    }

    private static string? GetFailureMode(HttpContext httpContext)
    {
        var cookieValue = httpContext.Request.Cookies[OAuthProviderFactory.UseMockProviderCookieName];
        if (cookieValue is null || !cookieValue.StartsWith(FailurePrefix, StringComparison.Ordinal))
        {
            return null;
        }

        return cookieValue[FailurePrefix.Length..];
    }

    private static string? GetMockEmailPrefix(HttpContext httpContext)
    {
        var cookieValue = httpContext.Request.Cookies[OAuthProviderFactory.UseMockProviderCookieName];
        if (cookieValue is null || cookieValue == "true" || cookieValue.StartsWith(FailurePrefix, StringComparison.Ordinal))
        {
            return null;
        }

        return cookieValue;
    }
}
