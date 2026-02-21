using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using PlatformPlatform.SharedKernel.Authentication.TokenSigning;

namespace PlatformPlatform.SharedKernel.Authentication.TokenGeneration;

public sealed class AccessTokenGenerator(ITokenSigningClient tokenSigningClient, TimeProvider timeProvider)
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
                    new Claim("tenant_id", userInfo.TenantId!.ToString()),
                    new Claim("tenant_name", userInfo.TenantName ?? string.Empty),
                    new Claim("tenant_logo_url", userInfo.TenantLogoUrl ?? string.Empty),
                    new Claim("subscription_plan", userInfo.SubscriptionPlan ?? string.Empty),
                    new Claim("title", userInfo.Title ?? string.Empty),
                    new Claim("avatar_url", userInfo.AvatarUrl ?? string.Empty),
                    new Claim("locale", userInfo.Locale!),
                    new Claim("session_id", userInfo.SessionId?.ToString() ?? string.Empty)
                ]
            )
        };

        var now = timeProvider.GetUtcNow();
        return tokenDescriptor.GenerateToken(
            now,
            now.AddMinutes(ValidForMinutes),
            tokenSigningClient.Issuer,
            tokenSigningClient.Audience,
            tokenSigningClient.GetSigningCredentials()
        );
    }
}
