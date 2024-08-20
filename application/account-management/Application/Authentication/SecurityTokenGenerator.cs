using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using PlatformPlatform.SharedKernel.ApplicationCore.Authentication;

namespace PlatformPlatform.AccountManagement.Application.Authentication;

public sealed class SecurityTokenGenerator(SecurityTokenSettings securityTokenSettings)
{
    public RefreshToken GenerateRefreshToken(User user)
    {
        return new RefreshToken { UserId = user.Id };
    }

    public string GenerateAccessToken(User user)
    {
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                    new Claim("Id", Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email),
                    new Claim(JwtRegisteredClaimNames.GivenName, user.FirstName ?? string.Empty),
                    new Claim(JwtRegisteredClaimNames.FamilyName, user.LastName ?? string.Empty),
                    new Claim("tenantId", user.TenantId),
                    new Claim("role", user.Role.ToString()),
                    new Claim("locale", "en"),
                    new Claim("title", user.Title ?? string.Empty),
                    new Claim("avatarUrl", user.Avatar.Url ?? string.Empty)
                ]
            ),
            Expires = DateTime.UtcNow.AddMinutes(5),
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
