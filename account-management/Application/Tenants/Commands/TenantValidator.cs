using FluentValidation;
using JetBrains.Annotations;
using PlatformPlatform.SharedKernel.ApplicationCore.Validation;

namespace PlatformPlatform.AccountManagement.Application.Tenants.Commands;

public interface ITenantValidation
{
    string Name { get; }

    string Email { get; }

    string? Phone { get; }
}

[UsedImplicitly]
public abstract class TenantValidator<T> : AbstractValidator<T> where T : ITenantValidation
{
    protected TenantValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Name).Length(1, 30).When(x => !string.IsNullOrEmpty(x.Name));
        RuleFor(x => x.Email).NotEmpty().SetValidator(new SharedValidations.Email());
        RuleFor(x => x.Phone).SetValidator(new SharedValidations.Phone());
    }
}