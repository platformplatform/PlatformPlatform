using Microsoft.IdentityModel.Tokens;

namespace PlatformPlatform.SharedKernel.Authentication.TokenSigning;

public interface ITokenSigningClient
{
    string Issuer { get; }

    string Audience { get; }

    SigningCredentials GetSigningCredentials();

    TokenValidationParameters GetTokenValidationParameters(TimeSpan clockSkew, bool validateLifetime);
}
