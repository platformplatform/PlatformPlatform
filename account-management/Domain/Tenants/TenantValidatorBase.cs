using FluentValidation;
using PlatformPlatform.Foundation.DomainModeling.Validation;

namespace PlatformPlatform.AccountManagement.Domain.Tenants;

public static class TenantPropertyValidation
{
    public static string SubdomainUniqueErrorMessage => "The subdomain must be unique.";

    public static string SubdomainRuleErrorMessage => "Subdomain must be alphanumeric and lowercase.";

    public sealed class Name : AbstractValidator<string>
    {
        public Name()
        {
            RuleFor(name => name).NotEmpty().WithName(nameof(Name));
            RuleFor(name => name).Length(1, 30).WithName(nameof(Name)).When(name => !string.IsNullOrEmpty(name));
        }
    }

    public sealed class Subdomain : AbstractValidator<string>
    {
        public Subdomain()
        {
            RuleFor(subdomain => subdomain).NotEmpty().WithName(nameof(Subdomain));
            RuleFor(subdomain => subdomain)
                .Length(3, 30)
                .Matches(@"^[a-z0-9]+$").WithMessage(SubdomainRuleErrorMessage)
                .WithName(nameof(Subdomain))
                .When(subdomain => !string.IsNullOrEmpty(subdomain));
        }
    }

    public sealed class Email : AbstractValidator<string>
    {
        public Email()
        {
            RuleFor(email => email).NotEmpty().WithName("Email");
            RuleFor(email => email)
                .EmailAddress()
                .MaximumLength(ValidationUtils.EmailMaxLength)
                .WithName(nameof(Email))
                .When(email => !string.IsNullOrEmpty(email));
        }
    }

    public sealed class Phone : AbstractValidator<string?>
    {
        public Phone()
        {
            RuleFor(phone => phone)
                .MaximumLength(ValidationUtils.PhoneMaxLength)
                .Matches(@"^\+?(\d[\d-. ]+)?(\([\d-. ]+\))?[\d-. ]+\d$")
                .WithName(nameof(Phone))
                .When(phone => !string.IsNullOrEmpty(phone));
        }
    }
}

public sealed class TenantValidator : AbstractValidator<Tenant>
{
    public TenantValidator()
    {
        RuleFor(x => x.Name).SetValidator(new TenantPropertyValidation.Name());
        RuleFor(x => x.Subdomain).SetValidator(new TenantPropertyValidation.Subdomain());
        RuleFor(x => x.Email).SetValidator(new TenantPropertyValidation.Email());
        RuleFor(x => x.Phone).SetValidator(new TenantPropertyValidation.Phone());
    }
}