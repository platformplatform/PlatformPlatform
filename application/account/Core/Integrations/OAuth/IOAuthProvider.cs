using PlatformPlatform.Account.Features.ExternalAuthentication.Domain;

namespace PlatformPlatform.Account.Integrations.OAuth;

public interface IOAuthProvider
{
    ExternalProviderType ProviderType { get; }

    string BuildAuthorizationUrl(string stateToken, string codeChallenge, string nonce, string redirectUri);

    Task<OAuthTokenResponse?> ExchangeCodeForTokensAsync(string code, string codeVerifier, string redirectUri, CancellationToken cancellationToken);

    Task<OAuthUserProfile?> GetUserProfileAsync(OAuthTokenResponse tokenResponse, CancellationToken cancellationToken);
}

public sealed record OAuthTokenResponse(string AccessToken, string? IdToken, int ExpiresIn);

public sealed record OAuthUserProfile(
    string ProviderUserId,
    string Email,
    bool EmailVerified,
    string? FirstName,
    string? LastName,
    string? AvatarUrl,
    string? Locale,
    string? Nonce
);
