using System.Text;

namespace PlatformPlatform.SharedKernel.ApplicationCore.Authentication;

public class AuthenticationTokenSettings
{
    public required string Issuer { get; init; }

    public required string Audience { get; init; }

    public required string Key { get; init; }

    public byte[] GetKeyBytes()
    {
        return Encoding.UTF8.GetBytes(Key);
    }
}
