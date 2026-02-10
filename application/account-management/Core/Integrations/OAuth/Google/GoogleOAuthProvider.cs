using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using PlatformPlatform.AccountManagement.Features.ExternalAuthentication.Domain;
using PlatformPlatform.SharedKernel.OpenIdConnect;

namespace PlatformPlatform.AccountManagement.Integrations.OAuth.Google;

internal sealed record GoogleOAuthConfiguration(string ClientId, string ClientSecret);

public sealed class GoogleOAuthProvider(HttpClient httpClient, IConfiguration configuration, OpenIdConnectConfigurationManagerFactory openIdConnectConfigurationManagerFactory, ILogger<GoogleOAuthProvider> logger) : IOAuthProvider
{
    private const string AuthorizationEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
    private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
    private const string GoogleDomain = "accounts.google.com";
    private const string GoogleDiscoveryUrl = $"https://{GoogleDomain}/.well-known/openid-configuration";
    private static readonly JsonWebTokenHandler TokenHandler = new();

    private readonly GoogleOAuthConfiguration _configuration = configuration.GetSection("OAuth:Google").Get<GoogleOAuthConfiguration>()
                                                               ?? throw new InvalidOperationException("OAuth:Google configuration is missing.");

    public ExternalProviderType ProviderType => ExternalProviderType.Google;

    public string BuildAuthorizationUrl(string stateToken, string codeChallenge, string nonce, string redirectUri)
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
            ["nonce"] = nonce,
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
            if (!response.IsSuccessStatusCode)
            {
                await LogTokenExchangeError(response, cancellationToken);
                return null;
            }

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

    public async Task<OAuthUserProfile?> GetUserProfileAsync(OAuthTokenResponse tokenResponse, CancellationToken cancellationToken)
    {
        if (tokenResponse.IdToken is null) return null;

        if (!TokenHandler.CanReadToken(tokenResponse.IdToken)) return null;

        var configurationManager = openIdConnectConfigurationManagerFactory.GetOrCreate(GoogleDiscoveryUrl);
        var openIdConfiguration = await configurationManager.GetConfigurationAsync(cancellationToken);

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            // Google ID tokens may contain either https://accounts.google.com or accounts.google.com as iss claim (see github.com/coreos/go-oidc/issues/125)
            ValidIssuers = [$"https://{GoogleDomain}", GoogleDomain],
            ValidateAudience = true,
            ValidAudiences = [_configuration.ClientId],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(10), // Both Azure and Google use NTP-synced clocks, so minimal skew is safe
            IssuerSigningKeys = openIdConfiguration.SigningKeys,
            ValidateIssuerSigningKey = true,
            ValidAlgorithms = [SecurityAlgorithms.RsaSha256]
        };

        var validationResult = await TokenHandler.ValidateTokenAsync(tokenResponse.IdToken, validationParameters);

        if (!validationResult.IsValid)
        {
            logger.LogError(validationResult.Exception, "Google ID token validation failed");
            return null;
        }

        var token = (JsonWebToken)validationResult.SecurityToken;

        if (!ValidateAccessTokenHash(token, tokenResponse.AccessToken))
        {
            return null;
        }

        var authorizedParty = token.Claims.FirstOrDefault(c => c.Type == "azp")?.Value;
        if (!string.IsNullOrEmpty(authorizedParty) && authorizedParty != _configuration.ClientId)
        {
            logger.LogError("azp claim mismatch. Expected: {Expected}, Got: {Actual}", _configuration.ClientId, authorizedParty);
            return null;
        }

        var subject = token.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
        var email = token.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
        var emailVerifiedClaim = token.Claims.FirstOrDefault(c => c.Type == "email_verified")?.Value;
        var givenName = token.Claims.FirstOrDefault(c => c.Type == "given_name")?.Value;
        var familyName = token.Claims.FirstOrDefault(c => c.Type == "family_name")?.Value;
        var picture = token.Claims.FirstOrDefault(c => c.Type == "picture")?.Value;
        var locale = token.Claims.FirstOrDefault(c => c.Type == "locale")?.Value;
        var nonce = token.Claims.FirstOrDefault(c => c.Type == "nonce")?.Value;

        if (string.IsNullOrEmpty(subject) || string.IsNullOrEmpty(email)) return null;

        var emailVerified = emailVerifiedClaim?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

        return new OAuthUserProfile(
            subject,
            email,
            emailVerified,
            givenName,
            familyName,
            picture,
            locale,
            nonce
        );
    }

    private bool ValidateAccessTokenHash(JsonWebToken idToken, string accessToken)
    {
        var atHash = idToken.Claims.FirstOrDefault(c => c.Type == "at_hash")?.Value;
        if (string.IsNullOrEmpty(atHash))
        {
            logger.LogWarning("Google ID token missing required at_hash claim when access token is present");
            return false;
        }

        var expectedHash = ComputeAtHash(accessToken, idToken.Alg);
        if (expectedHash is null)
        {
            logger.LogWarning("Unsupported signature algorithm '{Algorithm}' for at_hash validation", idToken.Alg);
            return false;
        }

        return atHash == expectedHash;
    }

    public static string? ComputeAtHash(string accessToken, string algorithm)
    {
        using var hashAlgorithm = algorithm switch
        {
            "RS256" => (HashAlgorithm)SHA256.Create(),
            "RS384" => SHA384.Create(),
            "RS512" => SHA512.Create(),
            _ => null
        };

        if (hashAlgorithm is null) return null;

        var hash = hashAlgorithm.ComputeHash(Encoding.ASCII.GetBytes(accessToken));
        var leftHalf = hash[..(hash.Length / 2)];
        return Base64UrlEncoder.Encode(leftHalf);
    }

    private async Task LogTokenExchangeError(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        const int maxBodyLength = 500;
        var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);

        try
        {
            using var document = JsonDocument.Parse(errorBody);
            var error = document.RootElement.TryGetProperty("error", out var errorElement) ? errorElement.GetString() : null;
            var errorDescription = document.RootElement.TryGetProperty("error_description", out var descriptionElement) ? descriptionElement.GetString() : null;

            logger.LogWarning("Google token exchange failed with status '{StatusCode}', error '{Error}': {ErrorDescription}", response.StatusCode, error, errorDescription);
        }
        catch (JsonException)
        {
            var truncatedBody = errorBody.Length > maxBodyLength ? errorBody[..maxBodyLength] : errorBody;
            logger.LogWarning("Google token exchange failed with status '{StatusCode}': {ErrorBody}", response.StatusCode, truncatedBody);
        }
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
