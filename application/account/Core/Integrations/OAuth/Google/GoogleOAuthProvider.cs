using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using PlatformPlatform.Account.Features.ExternalAuthentication.Domain;

namespace PlatformPlatform.Account.Integrations.OAuth.Google;

internal sealed record GoogleOAuthConfiguration(string ClientId, string ClientSecret);

public sealed class GoogleOAuthProvider(HttpClient httpClient, IConfiguration configuration) : IOAuthProvider
{
    private const string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";

    private readonly GoogleOAuthConfiguration _configuration = configuration.GetSection("OAuth:Google").Get<GoogleOAuthConfiguration>()
                                                               ?? throw new InvalidOperationException("OAuth:Google configuration is missing.");

    public ExternalProviderType ProviderType => ExternalProviderType.Google;

    public string BuildAuthorizationUrl(string stateToken, string codeChallenge, string redirectUri)
    {
        var parameters = new Dictionary<string, string>
        {
            ["client_id"] = _configuration.ClientId,
            ["redirect_uri"] = redirectUri,
            ["response_type"] = "code",
            ["scope"] = "openid email profile",
            ["state"] = stateToken,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256",
            ["access_type"] = "offline",
            ["prompt"] = "select_account"
        };

        var queryString = string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
        return $"{AuthorizationEndpoint}?{queryString}";
    }

    public async Task<OAuthTokenResponse?> ExchangeCodeForTokensAsync(string code, string codeVerifier, string redirectUri, CancellationToken cancellationToken)
    {
        try
        {
            var tokenRequest = new FormUrlEncodedContent([
                    new KeyValuePair<string, string>("grant_type", "authorization_code"),
                    new KeyValuePair<string, string>("code", code),
                    new KeyValuePair<string, string>("client_id", _configuration.ClientId),
                    new KeyValuePair<string, string>("client_secret", _configuration.ClientSecret),
                    new KeyValuePair<string, string>("redirect_uri", redirectUri),
                    new KeyValuePair<string, string>("code_verifier", codeVerifier)
                ]
            );

            var response = await httpClient.PostAsync(TokenEndpoint, tokenRequest, cancellationToken);
            if (!response.IsSuccessStatusCode) return null;

            var tokenResponse = await response.Content.ReadFromJsonAsync<GoogleTokenResponse>(cancellationToken);
            if (tokenResponse is null) return null;

            return new OAuthTokenResponse(tokenResponse.AccessToken, tokenResponse.IdToken, tokenResponse.ExpiresIn);
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    public OAuthUserProfile? GetUserProfile(OAuthTokenResponse tokenResponse)
    {
        if (tokenResponse.IdToken is null) return null;

        var jwtHandler = new JwtSecurityTokenHandler();
        if (!jwtHandler.CanReadToken(tokenResponse.IdToken)) return null;

        var token = jwtHandler.ReadJwtToken(tokenResponse.IdToken);

        var subject = token.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        var email = token.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
        var emailVerifiedClaim = token.Claims.FirstOrDefault(c => c.Type == "email_verified")?.Value;
        var givenName = token.Claims.FirstOrDefault(c => c.Type == "given_name")?.Value;
        var familyName = token.Claims.FirstOrDefault(c => c.Type == "family_name")?.Value;
        var picture = token.Claims.FirstOrDefault(c => c.Type == "picture")?.Value;
        var locale = token.Claims.FirstOrDefault(c => c.Type == "locale")?.Value;

        if (string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(email)) return null;

        var emailVerified = emailVerifiedClaim?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

        return new OAuthUserProfile(
            subject,
            email,
            emailVerified,
            givenName,
            familyName,
            picture,
            locale
        );
    }

    private sealed record GoogleTokenResponse(
        [property: JsonPropertyName("access_token")]
        string AccessToken,
        [property: JsonPropertyName("id_token")]
        string? IdToken,
        [property: JsonPropertyName("expires_in")]
        int ExpiresIn,
        [property: JsonPropertyName("token_type")]
        string TokenType,
        [property: JsonPropertyName("scope")] string Scope,
        [property: JsonPropertyName("refresh_token")]
        string? RefreshToken
    );
}
