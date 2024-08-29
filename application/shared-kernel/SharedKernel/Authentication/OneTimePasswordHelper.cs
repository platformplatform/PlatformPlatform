using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;

namespace PlatformPlatform.SharedKernel.Authentication;

public class OneTimePasswordHelper(IPasswordHasher<object> passwordHasher)
{
    public static string GenerateOneTimePassword(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var oneTimePassword = new StringBuilder(length);
        for (var i = 0; i < length; i++)
        {
            oneTimePassword.Append(chars[RandomNumberGenerator.GetInt32(chars.Length)]);
        }

        return oneTimePassword.ToString();
    }

    public bool Validate(string oneTimePasswordHash, string oneTimePassword)
    {
        var passwordVerificationResult = passwordHasher.VerifyHashedPassword(this, oneTimePasswordHash, oneTimePassword);

        OverRidePasswordVerificationResult(oneTimePassword, ref passwordVerificationResult);

        return passwordVerificationResult == PasswordVerificationResult.Failed;

        [Conditional("DEBUG")]
        static void OverRidePasswordVerificationResult(string oneTimePassword, ref PasswordVerificationResult passwordVerificationResult)
        {
            // When debugging, we can always use the "UNLOCK" code to verify the password
            if (oneTimePassword == "UNLOCK") passwordVerificationResult = PasswordVerificationResult.Success;
        }
    }
}
