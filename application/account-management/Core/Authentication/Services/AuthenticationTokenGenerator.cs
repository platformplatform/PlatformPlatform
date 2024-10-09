using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using PlatformPlatform.AccountManagement.Users.Domain;
using PlatformPlatform.SharedKernel.Authentication;

namespace PlatformPlatform.AccountManagement.Authentication.Services;

public sealed class AuthenticationTokenGenerator(ITokenSigningService tokenSigningService)
{
    public string GenerateRefreshToken(User user)
    {
        return GenerateRefreshToken(user, Guid.NewGuid().ToString(), 1, TimeProvider.System.GetUtcNow().AddMonths(3));
    }

    public string UpdateRefreshToken(User user, string refreshTokenChainId, int currentRefreshTokenVersion, DateTimeOffset expires)
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

        return GenerateToken(tokenDescriptor, expires);
    }

    public string GenerateAccessToken(User user)
    {
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                    new Claim(JwtRegisteredClaimNames.GivenName, user.FirstName ?? string.Empty),
                    new Claim(JwtRegisteredClaimNames.FamilyName, user.LastName ?? string.Empty),
                    new Claim(ClaimTypes.Role, user.Role.ToString()),
                    new Claim("tenant_id", user.TenantId),
                    new Claim("title", user.Title ?? string.Empty),
                    new Claim("avatar_url", user.Avatar.Url ?? string.Empty),
                    new Claim("locale", user.Locale)
                ]
            )
        };

        return GenerateToken(tokenDescriptor, TimeProvider.System.GetUtcNow().AddMinutes(5).UtcDateTime);
    }

    private string GenerateToken(SecurityTokenDescriptor tokenDescriptor, DateTimeOffset expires)
    {
        tokenDescriptor.Expires = expires.UtcDateTime;
        tokenDescriptor.Issuer = tokenSigningService.Issuer;
        tokenDescriptor.Audience = tokenSigningService.Audience;
        tokenDescriptor.SigningCredentials = tokenSigningService.GetSigningCredentials();

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(securityToken);
    }
}
