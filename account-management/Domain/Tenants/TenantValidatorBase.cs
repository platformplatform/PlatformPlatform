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
            RuleFor(email => email).NotEmpty().WithName("Email").SetValidator(new SharedValidations.Email());
        }
    }

    public sealed class TenantValidator : AbstractValidator<Tenant>
    {
        public TenantValidator()
        {
            RuleFor(x => x.Name).SetValidator(new Name());
            RuleFor(x => x.Subdomain).SetValidator(new Subdomain());
            RuleFor(x => x.Email).SetValidator(new Email());
            RuleFor(x => x.Phone).SetValidator(new SharedValidations.Phone());
        }
    }
}