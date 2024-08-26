using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using PlatformPlatform.SharedKernel.ApplicationCore.Authentication;

namespace PlatformPlatform.AccountManagement.Application.Authentication;

public sealed class AuthenticationTokenGenerator(AuthenticationTokenSettings authenticationTokenSettings)
{
    public string GenerateRefreshToken(UserId userId)
    {
        return GenerateRefreshToken(userId, Guid.NewGuid().ToString(), 1, TimeProvider.System.GetUtcNow().AddMonths(3));
    }

    public string UpdateRefreshToken(UserId userId, string refreshTokenChainId, int currentRefreshTokenVersion, DateTimeOffset expires)
    {
        return GenerateRefreshToken(userId, refreshTokenChainId, currentRefreshTokenVersion + 1, expires);
    }

    private string GenerateRefreshToken(UserId userId, string refreshTokenChainId, int refreshTokenVersion, DateTimeOffset expires)
    {
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Sub, userId),
                    new Claim("refresh_token_chain_id", refreshTokenChainId),
                    new Claim("refresh_token_version", refreshTokenVersion.ToString())
                ]
            ),
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
                    new Claim("tenant_id", user.TenantId),
                    new Claim("role", user.Role.ToString()),
                    new Claim("locale", "en"),
                    new Claim("title", user.Title ?? string.Empty),
                    new Claim("avatar_url", user.Avatar.Url ?? string.Empty)
                ]
            ),
        };

        return GenerateToken(tokenDescriptor, TimeProvider.System.GetUtcNow().AddMinutes(5).UtcDateTime);
    }

    private string GenerateToken(SecurityTokenDescriptor tokenDescriptor, DateTimeOffset expires)
    {
        tokenDescriptor.Expires = expires.UtcDateTime;
        tokenDescriptor.Issuer = authenticationTokenSettings.Issuer;
        tokenDescriptor.Audience = authenticationTokenSettings.Audience;
        tokenDescriptor.SigningCredentials = new SigningCredentials(
            new SymmetricSecurityKey(authenticationTokenSettings.GetKeyBytes()), SecurityAlgorithms.HmacSha512Signature
        );

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(securityToken);
    }
}
