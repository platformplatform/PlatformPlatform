using System.Text;

namespace PlatformPlatform.SharedKernel.ApplicationCore.Authentication;

public class SecurityTokenSettings
{
    public const string AuthenticationCookieName = "authentication-cookie";

    public const string RefreshTokenHttpHeaderKey = "X-Refresh-Token";

    public const string AccessTokenHttpHeaderKey = "X-Access-Token";

    public required string Issuer { get; init; }

    public required string Audience { get; init; }

    public required string Key { get; init; }

    public byte[] GetKeyBytes()
    {
        return Encoding.UTF8.GetBytes(Key);
    }
}
