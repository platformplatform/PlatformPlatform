using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace SharedKernel.OpenIdConnect;

public static class NonceUtilities
{
    public static string GenerateNonce()
    {
        return Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));
    }
}
