using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using PlatformPlatform.SharedKernel.Authentication.TokenSigning;

namespace PlatformPlatform.SharedKernel.Authentication.TokenGeneration;

public sealed class RefreshTokenGenerator(ITokenSigningClient tokenSigningClient, TimeProvider timeProvider)
{
    // Refresh tokens are stored as a persistent cookie in the user's browser.
    // Similar to Facebook and GitHub, when a user logs in, the session will be valid for a very long time.
    public const int ValidForHours = 2160; // 24 hours * 90 days

    public string Generate(UserInfo userInfo, SessionId sessionId, RefreshTokenJti jti)
    {
        return GenerateRefreshToken(userInfo, sessionId, jti, 1, timeProvider.GetUtcNow().AddHours(ValidForHours));
    }

    public string Generate(UserInfo userInfo, SessionId sessionId, RefreshTokenJti jti, DateTimeOffset expires)
    {
        return GenerateRefreshToken(userInfo, sessionId, jti, 1, expires);
    }

    public string Update(UserInfo userInfo, SessionId sessionId, RefreshTokenJti jti, int currentRefreshTokenVersion, DateTimeOffset expires)
    {
        return GenerateRefreshToken(userInfo, sessionId, jti, currentRefreshTokenVersion + 1, expires);
    }

    private string GenerateRefreshToken(UserInfo userInfo, SessionId sessionId, RefreshTokenJti jti, int refreshTokenVersion, DateTimeOffset expires)
    {
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            // Avoid changing these claims in the refresh token, as refresh tokens are valid for a very long time.
            // Changing this might have unintended side effects for already logged-in users.
            Subject = new ClaimsIdentity([
                    new Claim(JwtRegisteredClaimNames.Jti, jti),
                    new Claim(JwtRegisteredClaimNames.Sub, userInfo.Id!),
                    new Claim("tenant_id", userInfo.TenantId!.ToString()),
                    new Claim("sid", sessionId),
                    new Claim("ver", refreshTokenVersion.ToString())
                ]
            )
        };

        return tokenDescriptor.GenerateToken(
            timeProvider.GetUtcNow(),
            expires,
            tokenSigningClient.Issuer,
            tokenSigningClient.Audience,
            tokenSigningClient.GetSigningCredentials()
        );
    }
}
