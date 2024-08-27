using Microsoft.AspNetCore.Identity;

namespace PlatformPlatform.SharedKernel.ApplicationCore.Authentication;

public class OneTimePasswordValidator(IPasswordHasher<object> passwordHasher)
{
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
