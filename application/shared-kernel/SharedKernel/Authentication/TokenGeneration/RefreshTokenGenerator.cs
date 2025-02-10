using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using PlatformPlatform.SharedKernel.Authentication.TokenSigning;

namespace PlatformPlatform.SharedKernel.Authentication.TokenGeneration;

public sealed class RefreshTokenGenerator(ITokenSigningClient tokenSigningClient)
{
    // Refresh tokens are stored as a persistent cookie in the user's browser.
    // Similar to Facebook and GitHub, when a user logs in, the session will be valid for a very long time.
    private const int ValidForHours = 2160; // 24 hours * 90 days

    public string Generate(UserInfo userInfo)
    {
        return GenerateRefreshToken(userInfo, RefreshTokenId.NewId(), 1, TimeProvider.System.GetUtcNow().AddHours(ValidForHours));
    }

    public string Update(UserInfo userInfo, RefreshTokenId refreshTokenId, int currentRefreshTokenVersion, DateTimeOffset expires)
    {
        return GenerateRefreshToken(userInfo, refreshTokenId, currentRefreshTokenVersion + 1, expires);
    }

    private string GenerateRefreshToken(UserInfo userInfo, RefreshTokenId refreshTokenId, int refreshTokenVersion, DateTimeOffset expires)
    {
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            // Avoid changing these claims in the refresh token, as refresh tokens are valid for a very long time.
            // Changing this might have unintended side effects for already logged-in users.
            Subject = new ClaimsIdentity([
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Sub, userInfo.Id!),
                    new Claim("tenant_id", userInfo.TenantId!.ToString()),
                    new Claim("rtid", refreshTokenId),
                    new Claim("rtv", refreshTokenVersion.ToString())
                ]
            )
        };

        return tokenDescriptor.GenerateToken(
            expires,
            tokenSigningClient.Issuer,
            tokenSigningClient.Audience,
            tokenSigningClient.GetSigningCredentials()
        );
    }
}
