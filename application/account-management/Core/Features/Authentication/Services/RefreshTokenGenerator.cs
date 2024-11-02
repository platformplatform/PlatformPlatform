using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Authentication;

namespace PlatformPlatform.AccountManagement.Features.Authentication.Services;

public sealed class RefreshTokenGenerator(ITokenSigningService tokenSigningService)
{
    // Refresh tokens are stored as a persistent cookie in the user's browser.
    // Similar to Facebook and GitHub, when a user logs in, the session will be valid for a very long time.
    private const int ValidForHours = 2160; // 24 hours * 90 days

    public string Generate(User user)
    {
        return GenerateRefreshToken(user, Guid.NewGuid().ToString(), 1, TimeProvider.System.GetUtcNow().AddHours(ValidForHours));
    }

    public string Update(User user, string refreshTokenChainId, int currentRefreshTokenVersion, DateTimeOffset expires)
    {
        return GenerateRefreshToken(user, refreshTokenChainId, currentRefreshTokenVersion + 1, expires);
    }

    private string GenerateRefreshToken(User user, string refreshTokenChainId, int refreshTokenVersion, DateTimeOffset expires)
    {
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            // Avoid changing these claims in the refresh token, as refresh tokens are valid for a very long time.
            // Changing this might have unintended side effects for already logged-in users.
            Subject = new ClaimsIdentity([
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                    new Claim("tenant_id", user.TenantId),
                    new Claim("rtid", refreshTokenChainId),
                    new Claim("rtv", refreshTokenVersion.ToString())
                ]
            )
        };

        return tokenDescriptor.GenerateToken(
            expires,
            tokenSigningService.Issuer,
            tokenSigningService.Audience,
            tokenSigningService.GetSigningCredentials()
        );
    }
}
