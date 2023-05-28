using FluentValidation;

namespace PlatformPlatform.SharedKernel.DomainModeling.Validation;

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
            RuleFor(email => email)
                .EmailAddress()
                .MaximumLength(EmailMaxLength)
                .WithName(emailName)
                .When(email => !string.IsNullOrEmpty(email));
        }
    }

    public sealed class Phone : AbstractValidator<string?>
    {
        public Phone(string phoneName = nameof(Phone))
        {
            RuleFor(phone => phone)
                .MaximumLength(PhoneMaxLength)
                .Matches(@"^\+?(\d[\d-. ]+)?(\([\d-. ]+\))?[\d-. ]+\d$")
                .WithName(phoneName)
                .When(phone => !string.IsNullOrEmpty(phone));
        }
    }
}