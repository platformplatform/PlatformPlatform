using System.Text.RegularExpressions;

namespace PlatformPlatform.Foundation.DddCore;

/// <summary>
///     The ValidationUtils class is a static utility class that provides methods for validating various types of input
///     data such as strings, phone numbers, and email addresses. These methods are used for validating the data of
///     domain entities and commands. The methods return a <see cref="Result" /> object, which is used to determine if
///     the input is valid or not. If the input is not valid, the <see cref="Result" /> object contains an error message
///     that can be used to inform the user of the error.
/// </summary>
public static partial class ValidationUtils
{
    public static Result IsStringValid(string name, string input, int minLength, int maxLength, string errorMessage)
    {
        var isSuccess = input.Length >= minLength && input.Length <= maxLength;
        return GetResult(name, isSuccess, errorMessage);
    }

    public static Result IsStringValid(string name, string input, string regEx, int minLength, int maxLength,
        string errorMessage)
    {
        var isLengthOk = input.Length >= minLength && input.Length <= maxLength;
        var isRegExOk = Regex.IsMatch(input, regEx, RegexOptions.NonBacktracking);
        return GetResult(name, isLengthOk && isRegExOk, errorMessage);
    }

    public static Result IsValidPhone(string name, string? input)
    {
        // The ITU-T Recommendation E.164 limits phone numbers to 15 digits (including country code).
        // We add 5 extra characters to allow for spaces, dashes, parentheses, etc. 
        const int phoneMaxLength = 20;
        const string errorMessage = "Phone number must be a valid format and not exceed 20 digits.";

        var isLengthOk = input is null || input.Length <= phoneMaxLength;
        var isRegExOk = string.IsNullOrEmpty(input) || PhoneRegex().IsMatch(input);
        return GetResult(name, isLengthOk && isRegExOk, errorMessage);
    }

    [GeneratedRegex(@"^\+?(\d[\d-. ]+)?(\([\d-. ]+\))?[\d-. ]+\d$")]
    private static partial Regex PhoneRegex();

    public static Result IsValidEmail(string name, string input)
    {
        // While emails can be longer, we will limit them to 100 characters which should be enough for most cases
        const int emailMaxLength = 100;
        const string errorMessage = "Email must be a valid email address and not exceed 100 characters.";

        var isLengthOk = input.Length <= emailMaxLength;
        var isRegExOk = !string.IsNullOrWhiteSpace(input) && EmailRegex().IsMatch(input);
        return GetResult(name, isLengthOk && isRegExOk, errorMessage);
    }

    [GeneratedRegex(@"^[\w-]+(\.[\w-]+)*@([\w-]+\.)+[a-zA-Z]{2,7}$")]
    private static partial Regex EmailRegex();

    private static Result GetResult(string name, bool success, string errorMessage)
    {
        return success
            ? Result.Success()
            : Result.Failure(name, errorMessage);
    }
}