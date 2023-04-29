using System.Text.RegularExpressions;
using FluentValidation;

namespace PlatformPlatform.AccountManagement.Application.Shared.Validation;

public static class FluentValidationExtensions
{
    private static readonly Lazy<Regex> PhoneRegex =
        new(() => new Regex(@"^\+?(\d[\d-. ]+)?(\([\d-. ]+\))?[\d-. ]+\d$", RegexOptions.Compiled));

    public static IRuleBuilderOptions<T, string> Phone<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .Must(phone => string.IsNullOrEmpty(phone) || PhoneRegex.Value.IsMatch(phone))
            .WithMessage("The phone number format is not valid.");
    }
}