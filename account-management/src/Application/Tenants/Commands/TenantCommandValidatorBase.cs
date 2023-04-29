using System.Text.RegularExpressions;
using FluentValidation;
using PlatformPlatform.AccountManagement.Domain.Tenants;

namespace PlatformPlatform.AccountManagement.Application.Tenants.Commands;

public abstract class TenantCommandValidatorBase<T> : AbstractValidator<T> where T : ITenantCommand
{
    protected TenantCommandValidatorBase()
    {
        RuleFor(x => x.Name)
            .Length(TenantValidationConstants.NameMinLength, TenantValidationConstants.NameMaxLength);

        RuleFor(x => x.Email)
            .MaximumLength(TenantValidationConstants.EmailMaxLength)
            .EmailAddress();

        RuleFor(x => x.Phone)
            .MaximumLength(TenantValidationConstants.PhoneMaxLength)
            .Must(phone => Regex.IsMatch(phone, @"^\+?(\d[\d-. ]+)?(\([\d-. ]+\))?[\d-. ]+\d$"))
            .WithMessage("The phone number format is not valid.");
    }
}