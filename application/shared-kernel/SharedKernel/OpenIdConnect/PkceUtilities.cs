using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace PlatformPlatform.SharedKernel.OpenIdConnect;

public static class PkceUtilities
{
    public static string GenerateCodeVerifier()
    {
        return Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(96));
    }

    public static string GenerateCodeChallenge(string codeVerifier)
    {
        return Base64UrlEncoder.Encode(SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier)));
    }
}
