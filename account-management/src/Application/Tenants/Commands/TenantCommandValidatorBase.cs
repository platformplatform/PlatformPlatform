using FluentValidation;
using PlatformPlatform.AccountManagement.Application.Shared.Validation;
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

        When(x => x.Phone != null, () =>
        {
            RuleFor(x => x.Phone)
                .MaximumLength(TenantValidationConstants.PhoneMaxLength)
                .Phone();
        });
    }
}