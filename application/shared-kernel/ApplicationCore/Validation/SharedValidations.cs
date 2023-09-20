using FluentValidation;

namespace PlatformPlatform.SharedKernel.ApplicationCore.Validation;

public static class SharedValidations
{
    // While emails can be longer, we will limit them to 100 characters which should be enough for most cases
    private const int EmailMaxLength = 100;

    // The ITU-T Recommendation E.164 limits phone numbers to 15 digits (including country code).
    // We add 5 extra characters to allow for spaces, dashes, parentheses, etc. 
    private const int PhoneMaxLength = 20;

    public sealed class Email : AbstractValidator<string>
    {
        public Email(string emailName = nameof(Email))
        {
            const string errorMessage = "Email must be in a valid format and no longer than 100 characters.";
            RuleFor(email => email)
                .EmailAddress()
                .WithName(emailName)
                .WithMessage(errorMessage)
                .MaximumLength(EmailMaxLength)
                .WithMessage(errorMessage)
                .When(email => !string.IsNullOrEmpty(email));
        }
    }

    public sealed class Phone : AbstractValidator<string?>
    {
        public Phone(string phoneName = nameof(Phone))
        {
            const string errorMessage = "Phone must be in a valid format and no longer than 20 characters.";
            RuleFor(phone => phone)
                .MaximumLength(PhoneMaxLength)
                .WithName(phoneName)
                .WithMessage(errorMessage)
                .Matches(@"^\+?(\d[\d-. ]+)?(\([\d-. ]+\))?[\d-. ]+\d$")
                .WithMessage(errorMessage)
                .When(phone => !string.IsNullOrEmpty(phone));
        }
    }
}