using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace PlatformPlatform.SharedKernel.Authentication.TokenGeneration;

internal static class SecurityTokenDescriptorExtensions
{
    private static readonly JsonWebTokenHandler TokenHandler = new();

    extension(SecurityTokenDescriptor tokenDescriptor)
    {
        internal string GenerateToken(DateTimeOffset now, DateTimeOffset expires, string issuer, string audience, SigningCredentials signingCredentials)
        {
            tokenDescriptor.NotBefore = now.UtcDateTime;
            tokenDescriptor.IssuedAt = now.UtcDateTime;
            tokenDescriptor.Expires = expires.UtcDateTime;
            tokenDescriptor.Issuer = issuer;
            tokenDescriptor.Audience = audience;
            tokenDescriptor.SigningCredentials = signingCredentials;

            return TokenHandler.CreateToken(tokenDescriptor);
        }
    }
}
