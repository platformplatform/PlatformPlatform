using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Authentication;

namespace PlatformPlatform.AccountManagement.Features.Authentication.Services;

public sealed class RefreshTokenGenerator(ITokenSigningService tokenSigningService)
{
    public string Generate(User user)
    {
        return GenerateRefreshToken(user, Guid.NewGuid().ToString(), 1, TimeProvider.System.GetUtcNow().AddMonths(3));
    }

    public string Update(User user, string refreshTokenChainId, int currentRefreshTokenVersion, DateTimeOffset expires)
    {
        return GenerateRefreshToken(user, refreshTokenChainId, currentRefreshTokenVersion + 1, expires);
    }

    private string GenerateRefreshToken(User user, string refreshTokenChainId, int refreshTokenVersion, DateTimeOffset expires)
    {
        var tokenDescriptor = new SecurityTokenDescriptor
        {
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
