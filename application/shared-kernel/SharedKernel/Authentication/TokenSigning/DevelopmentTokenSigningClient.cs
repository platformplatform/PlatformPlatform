using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.IdentityModel.Tokens;

namespace PlatformPlatform.SharedKernel.Authentication.TokenSigning;

public class DevelopmentTokenSigningClient
    : ITokenSigningClient
{
    private byte[]? _key;

    private static Assembly EntryAssembly => Assembly.GetExecutingAssembly();

    private static string UserSecretsId => EntryAssembly.GetCustomAttribute<UserSecretsIdAttribute>()!.UserSecretsId;

    public string Issuer => "Localhost";

    public string Audience => "Localhost";

    public SigningCredentials GetSigningCredentials()
    {
        var key = new SymmetricSecurityKey(GetKey());
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
            IssuerSigningKey = new SymmetricSecurityKey(GetKey()),
            ClockSkew = clockSkew,
            ValidateLifetime = validateLifetime
        };
    }

    private byte[] GetKey()
    {
        if (_key is null)
        {
            var config = new ConfigurationBuilder().AddUserSecrets(UserSecretsId).Build();
            var base64Key = config["authentication-token-signing-key"]
                            ?? throw new InvalidOperationException("The signing key is not configured.");
            _key = Convert.FromBase64String(base64Key);
        }

        return _key;
    }
}
