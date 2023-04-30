using System.Text.RegularExpressions;

namespace PlatformPlatform.AccountManagement.Domain.Shared;

public static class ValidationUtils
{
    private static readonly Lazy<Regex> PhoneRegex =
        new(() => new Regex(@"^\+?(\d[\d-. ]+)?(\([\d-. ]+\))?[\d-. ]+\d$", RegexOptions.Compiled));

    private static readonly Lazy<Regex> EmailRegex =
        new(() => new Regex(@"^[\w-]+(\.[\w-]+)*@([\w-]+\.)+[a-zA-Z]{2,7}$", RegexOptions.Compiled));

    public static Result IsStringValid(string name, string input, int minLength, int maxLength, string errorMessage)
    {
        var isSuccess = input.Length >= minLength && input.Length <= maxLength;
        return GetResult(name, isSuccess, errorMessage);
    }

    public static Result IsStringValid(string name, string input, string regEx, int minLength, int maxLength,
        string errorMessage)
    {
        var isLengthOk = input.Length >= minLength && input.Length <= maxLength;
        var isRegExOk = Regex.IsMatch(input, regEx);
        return GetResult(name, isLengthOk && isRegExOk, errorMessage);
    }

    public static Result IsStringValid(string name, string input, string regEx, string errorMessage)
    {
        var isSuccess = Regex.IsMatch(input, regEx);
        return GetResult(name, isSuccess, errorMessage);
    }

    public static Result IsValidPhone(string name, string? input)
    {
        // The ITU-T Recommendation E.164 limits phone numbers to 15 digits (including country code).
        // We add 5 extra characters to allow for spaces, dashes, parentheses, etc. 
        const int phoneMaxLength = 20;
        const string errorMessage = "Phone number must be a valid format and not exceed 20 digits.";

        var isLengthOk = input is null || input.Length <= phoneMaxLength;
        var isRegExOk = string.IsNullOrEmpty(input) || PhoneRegex.Value.IsMatch(input);
        return GetResult(name, isLengthOk && isRegExOk, errorMessage);
    }

    public static Result IsValidEmail(string name, string input)
    {
        // While emails can be longer, we will limit them to 100 characters which should be enough for most cases
        const int emailMaxLength = 100;
        const string errorMessage = "Email must be a valid email address and not exceed 100 characters.";

        var isLengthOk = input.Length <= emailMaxLength;
        var isRegExOk = !string.IsNullOrWhiteSpace(input) && EmailRegex.Value.IsMatch(input);
        return GetResult(name, isLengthOk && isRegExOk, errorMessage);
    }

    private static Result GetResult(string name, bool success, string errorMessage)
    {
        return success
            ? Result.Success()
            : Result.Failure(name, errorMessage);
    }
}