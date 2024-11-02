using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Authentication;

namespace PlatformPlatform.AccountManagement.Features.Authentication.Services;

public sealed class AccessTokenGenerator(ITokenSigningService tokenSigningService)
{
    public string Generate(User user)
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

        return tokenDescriptor.GenerateToken(
            TimeProvider.System.GetUtcNow().AddMinutes(5).UtcDateTime,
            tokenSigningService.Issuer,
            tokenSigningService.Audience,
            tokenSigningService.GetSigningCredentials()
        );
    }
}
