using Azure.Security.KeyVault.Keys.Cryptography;
using Microsoft.IdentityModel.Tokens;
using PlatformPlatform.SharedKernel.ApplicationCore.Authentication;

namespace PlatformPlatform.SharedKernel.InfrastructureCore.Services;

public class AzureTokenSigningService(CryptographyClient cryptographyClient, string issuer, string audience)
    : ITokenSigningService
{
    public string Issuer => issuer;

    public string Audience => audience;

    public SigningCredentials GetSigningCredentials()
    {
        var rsaKey = new RsaSecurityKey(cryptographyClient.CreateRSA());
        return new SigningCredentials(rsaKey, SecurityAlgorithms.RsaSha256);
    }

    public TokenValidationParameters GetTokenValidationParameters(TimeSpan clockSkew, bool validateLifetime)
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(cryptographyClient.CreateRSA()),
            ClockSkew = clockSkew,
            ValidateLifetime = validateLifetime
        };
    }
}
