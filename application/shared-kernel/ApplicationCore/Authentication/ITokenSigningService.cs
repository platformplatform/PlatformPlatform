using Microsoft.IdentityModel.Tokens;

namespace PlatformPlatform.SharedKernel.ApplicationCore.Authentication;

public interface ITokenSigningService
{
    string Issuer { get; }

    string Audience { get; }

    SigningCredentials GetSigningCredentials();

    TokenValidationParameters GetTokenValidationParameters(TimeSpan clockSkew, bool validateLifetime);
}
