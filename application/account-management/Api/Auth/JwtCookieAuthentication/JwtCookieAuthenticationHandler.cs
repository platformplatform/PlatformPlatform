using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace PlatformPlatform.AccountManagement.Api.Auth.JwtCookieAuthentication;

public class JwtCookieAuthenticationHandler(
    IOptionsMonitor<JwtCookieAuthenticationOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder
)
    : SignInAuthenticationHandler<JwtCookieAuthenticationOptions>(options, logger, encoder)
{
    private static readonly CookieBuilder Cookie = new RequestPathBaseCookieBuilder
    {
        SameSite = SameSiteMode.Strict,
        HttpOnly = true,
        SecurePolicy = CookieSecurePolicy.Always,
        IsEssential = true
    };

    /// <inheritdoc />
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        Request.Cookies.TryGetValue(Options.AccessTokenName, out var accessToken);
        Request.Cookies.TryGetValue(Options.RefreshTokenName, out var refreshToken);

        if (accessToken is null || refreshToken is null)
        {
            return AuthenticateResults.FailedMissingCookies;
        }

        var tokenHandler = new JwtSecurityTokenHandler();

        var tokenValidationResult =
            await tokenHandler.ValidateTokenAsync(accessToken, Options.TokenValidationParameters);

        if (tokenValidationResult.IsValid)
        {
            var user = new ClaimsPrincipal(tokenValidationResult.ClaimsIdentity);
            return AuthenticateResult.Success(new AuthenticationTicket(user, Scheme.Name));
        }

        // Refresh token

        return AuthenticateResults.FailedInvalidAccessToken;
    }

    /// <inheritdoc />
    protected override Task HandleSignInAsync(ClaimsPrincipal user, AuthenticationProperties? properties)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtSigningCredentials = new SigningCredentials(Options.SigningSecurityKey, SecurityAlgorithms.HmacSha256);

        var utcNow = DateTime.UtcNow;
        var accessToken = CreateAccessToken(user, tokenHandler, utcNow, jwtSigningCredentials);
        var refreshTokenTicket = CreateRefreshTicket(user, utcNow, properties);
        var refreshToken = Options.RefreshTokenProtector.Protect(refreshTokenTicket, GetTlsTokenBinding());

        var cookieOptions = Cookie.Build(Context);

        Response.Cookies.Append(Options.AccessTokenName, accessToken, cookieOptions);
        Response.Cookies.Append(Options.RefreshTokenName, refreshToken, cookieOptions);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override Task HandleSignOutAsync(AuthenticationProperties? properties)
    {
        Response.Cookies.Delete(Options.AccessTokenName);
        Response.Cookies.Delete(Options.RefreshTokenName);
        return Task.CompletedTask;
    }

    private AuthenticationTicket CreateRefreshTicket(
        ClaimsPrincipal user,
        DateTime utcNow,
        AuthenticationProperties? properties
    )
    {
        var refreshTokenDetails = new RefreshTokenDetails(properties?.Items)
        {
            UserId = user.FindFirstValue(ClaimTypes.NameIdentifier)
        };
        var refreshProperties = new AuthenticationProperties(refreshTokenDetails.ToDictionary())
        {
            ExpiresUtc = utcNow.Add(Options.RefreshTokenExpireTimeSpan)
        };

        return new AuthenticationTicket(user, refreshProperties, $"{Scheme.Name};RefreshToken");
    }

    private string CreateAccessToken(
        ClaimsPrincipal user,
        JwtSecurityTokenHandler tokenHandler,
        DateTime utcNow,
        SigningCredentials signingCredentials
    )
    {
        var accessSecurityToken = tokenHandler.CreateToken(new SecurityTokenDescriptor
        {
            Subject = user.Identity as ClaimsIdentity,
            Audience = Options.TokenValidationParameters.ValidAudience,
            Issuer = Options.TokenValidationParameters.ValidIssuer,
            IssuedAt = utcNow,
            Expires = utcNow.Add(Options.AccessTokenExpireTimeSpan),
            NotBefore = utcNow.Add(Options.NotBeforeTimeSpan),
            SigningCredentials = signingCredentials
            // EncryptingCredentials = jwtEncryptingCredentials, // Add encryption
        });
        return tokenHandler.WriteToken(accessSecurityToken);
    }

    private string? GetTlsTokenBinding()
    {
        var binding = Context.Features.Get<ITlsTokenBindingFeature>()?.GetProvidedTokenBindingId();
        return binding == null ? null : Convert.ToBase64String(binding);
    }
}