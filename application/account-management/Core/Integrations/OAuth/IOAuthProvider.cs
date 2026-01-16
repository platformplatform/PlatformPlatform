using PlatformPlatform.AccountManagement.Features.ExternalAuthentication.Domain;

namespace PlatformPlatform.AccountManagement.Integrations.OAuth;

public interface IOAuthProvider
{
    ExternalProviderType ProviderType { get; }

    string BuildAuthorizationUrl(string stateToken, string codeChallenge, string redirectUri);

    Task<OAuthTokenResponse?> ExchangeCodeForTokensAsync(string code, string codeVerifier, string redirectUri, CancellationToken cancellationToken);

    OAuthUserProfile? GetUserProfile(OAuthTokenResponse tokenResponse);
}

public sealed record OAuthTokenResponse(string AccessToken, string? IdToken, int ExpiresIn);

public sealed record OAuthUserProfile(
    string ProviderUserId,
    string Email,
    bool EmailVerified,
    string? FirstName,
    string? LastName,
    string? AvatarUrl,
    string? Locale
);
