using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using PlatformPlatform.AccountManagement.Features.ExternalAuthentication.Domain;

namespace PlatformPlatform.AccountManagement.Integrations.OAuth.Mock;

public sealed class MockOAuthProvider(IConfiguration configuration, IHttpContextAccessor httpContextAccessor, OAuthProviderFactory oauthProviderFactory) : IOAuthProvider
{
    public const string MockEmail = "mockuser@test.com";
    public const string MockProviderUserId = "mock-google-user-id-12345";
    public const string MockFirstName = "Mock";
    public const string MockLastName = "User";

    private readonly bool _isEnabled = configuration.GetValue<bool>("OAuth:AllowMockProvider");

    public ExternalProviderType ProviderType => ExternalProviderType.Google;

    public string BuildAuthorizationUrl(string stateToken, string codeChallenge, string redirectUri)
    {
        if (!_isEnabled)
        {
            throw new InvalidOperationException("Mock OAuth provider is not enabled.");
        }

        return $"{redirectUri}?code=mock-authorization-code&state={Uri.EscapeDataString(stateToken)}";
    }

    public Task<OAuthTokenResponse?> ExchangeCodeForTokensAsync(string code, string codeVerifier, string redirectUri, CancellationToken cancellationToken)
    {
        if (!_isEnabled)
        {
            throw new InvalidOperationException("Mock OAuth provider is not enabled.");
        }

        var mockTokenResponse = new OAuthTokenResponse(
            "mock-access-token",
            CreateMockIdToken(),
            3600
        );

        return Task.FromResult<OAuthTokenResponse?>(mockTokenResponse);
    }

    public OAuthUserProfile GetUserProfile(OAuthTokenResponse tokenResponse)
    {
        if (!_isEnabled)
        {
            throw new InvalidOperationException("Mock OAuth provider is not enabled.");
        }

        var emailPrefix = oauthProviderFactory.GetMockEmailPrefix(httpContextAccessor.HttpContext!);
        var email = emailPrefix is not null ? $"{emailPrefix}@test.com" : MockEmail;
        var providerUserId = emailPrefix is not null ? $"mock-google-{emailPrefix}" : MockProviderUserId;

        return new OAuthUserProfile(
            providerUserId,
            email,
            true,
            MockFirstName,
            MockLastName,
            null,
            "en"
        );
    }

    private static string CreateMockIdToken()
    {
        return "mock-id-token";
    }
}
