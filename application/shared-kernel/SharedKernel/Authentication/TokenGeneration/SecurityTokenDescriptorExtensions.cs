using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace PlatformPlatform.SharedKernel.Authentication.TokenGeneration;

internal static class SecurityTokenDescriptorExtensions
{
    internal static string GenerateToken(
        this SecurityTokenDescriptor tokenDescriptor,
        DateTimeOffset expires,
        string issuer,
        string audience,
        SigningCredentials signingCredentials
    )
    {
        tokenDescriptor.Expires = expires.UtcDateTime;
        tokenDescriptor.Issuer = issuer;
        tokenDescriptor.Audience = audience;
        tokenDescriptor.SigningCredentials = signingCredentials;

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(securityToken);
    }
}
