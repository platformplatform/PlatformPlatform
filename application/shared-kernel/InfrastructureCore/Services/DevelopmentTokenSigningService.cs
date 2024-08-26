using Microsoft.IdentityModel.Tokens;
using PlatformPlatform.SharedKernel.ApplicationCore.Authentication;

namespace PlatformPlatform.SharedKernel.InfrastructureCore.Services;

public class DevelopmentTokenSigningService
    : ITokenSigningService
{
    private byte[] Key { get; } = "put-64-bytes-key-here"u8.ToArray();

    public string Issuer => "https://localhost:9000";

    public string Audience => "https://localhost:9000";

    public SigningCredentials GetSigningCredentials()
    {
        var key = new SymmetricSecurityKey(Key);
        return new SigningCredentials(key, SecurityAlgorithms.HmacSha512);
    }

    public TokenValidationParameters GetTokenValidationParameters(TimeSpan clockSkew, bool validateLifetime)
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = Issuer,
            ValidateAudience = true,
            ValidAudience = Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Key),
            ClockSkew = clockSkew,
            ValidateLifetime = validateLifetime
        };
    }
}
