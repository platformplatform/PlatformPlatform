using Microsoft.IdentityModel.Tokens;

namespace PlatformPlatform.SharedKernel.Authentication;

public interface ITokenSigningService
{
    string Issuer { get; }

    string Audience { get; }

    SigningCredentials GetSigningCredentials();

    TokenValidationParameters GetTokenValidationParameters(TimeSpan clockSkew, bool validateLifetime);
}
