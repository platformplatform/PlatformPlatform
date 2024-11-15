using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace PlatformPlatform.SharedKernel.Authentication;

public sealed class AccessTokenGenerator(ITokenSigningService tokenSigningService)
{
    // Access tokens should only be valid for a very short time and cannot be revoked.
    // For example, if a user gets a new role, the changes will not take effect until the access token expires.
    private const int ValidForMinutes = 5;

    public string Generate(UserInfo userInfo)
    {
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                    new Claim(JwtRegisteredClaimNames.Sub, userInfo.Id!),
                    new Claim(JwtRegisteredClaimNames.Email, userInfo.Email!),
                    new Claim(JwtRegisteredClaimNames.GivenName, userInfo.FirstName ?? string.Empty),
                    new Claim(JwtRegisteredClaimNames.FamilyName, userInfo.LastName ?? string.Empty),
                    new Claim(ClaimTypes.Role, userInfo.Role!),
                    new Claim("tenant_id", userInfo.TenantId!),
                    new Claim("title", userInfo.Title ?? string.Empty),
                    new Claim("avatar_url", userInfo.AvatarUrl ?? string.Empty),
                    new Claim("locale", userInfo.Locale!)
                ]
            )
        };

        return tokenDescriptor.GenerateToken(
            TimeProvider.System.GetUtcNow().AddMinutes(ValidForMinutes).UtcDateTime,
            tokenSigningService.Issuer,
            tokenSigningService.Audience,
            tokenSigningService.GetSigningCredentials()
        );
    }
}
