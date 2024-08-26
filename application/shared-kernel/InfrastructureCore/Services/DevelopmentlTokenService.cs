using Microsoft.IdentityModel.Tokens;
using PlatformPlatform.SharedKernel.ApplicationCore.Authentication;

namespace PlatformPlatform.SharedKernel.InfrastructureCore.Services;

public class DevelopmentTokenSigningService(AuthenticationTokenSettings settings)
    : ITokenSigningService
{
    public string Issuer => settings.Issuer;

    public string Audience => settings.Audience;

    public SigningCredentials GetSigningCredentials()
    {
        var key = new SymmetricSecurityKey(settings.GetKeyBytes());
        return new SigningCredentials(key, SecurityAlgorithms.HmacSha512);
    }

    public TokenValidationParameters GetTokenValidationParameters(TimeSpan clockSkew, bool validateLifetime)
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = settings.Issuer,
            ValidateAudience = true,
            ValidAudience = settings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(settings.GetKeyBytes()),
            ClockSkew = clockSkew,
            ValidateLifetime = validateLifetime
        };
    }
}
