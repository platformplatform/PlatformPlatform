using FluentValidation;
using PlatformPlatform.SharedKernel.ApplicationCore.Validation;

namespace PlatformPlatform.AccountManagement.Application.Tenants;

public interface ITenantValidation
{
    string Name { get; }

    string? Phone { get; }
}

[UsedImplicitly]
public abstract class TenantValidator<T> : AbstractValidator<T> where T : ITenantValidation
{
    protected TenantValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Name).Length(1, 30)
            .WithMessage("Name must be between 1 and 30 characters.")
            .When(x => !string.IsNullOrEmpty(x.Name));
        RuleFor(x => x.Phone).SetValidator(new SharedValidations.Phone());
    }
}