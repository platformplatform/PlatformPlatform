using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using PlatformPlatform.SharedKernel.ApplicationCore.Authentication;

namespace PlatformPlatform.AccountManagement.Application.Authentication;

public sealed class SecurityTokenGenerator(SecurityTokenSettings securityTokenSettings)
{
    public string GenerateRefreshToken(User user)
    {
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                    new Claim("refresh_token_chain_id", Guid.NewGuid().ToString()),
                    new Claim("refresh_token_version", 1.ToString())
                ]
            ),
            Expires = TimeProvider.System.GetUtcNow().AddMonths(3).UtcDateTime,
            Issuer = securityTokenSettings.Issuer,
            Audience = securityTokenSettings.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(securityTokenSettings.GetKeyBytes()), SecurityAlgorithms.HmacSha512Signature
            )
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(securityToken);
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
            Expires = TimeProvider.System.GetUtcNow().AddMinutes(5).UtcDateTime,
            Issuer = securityTokenSettings.Issuer,
            Audience = securityTokenSettings.Audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(securityTokenSettings.GetKeyBytes()), SecurityAlgorithms.HmacSha512Signature
            )
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(securityToken);
    }
}
