using FluentValidation;
using PlatformPlatform.SharedKernel.ApplicationCore.Validation;

namespace PlatformPlatform.AccountManagement.Application.Users;

public interface IUserValidation
{
    string Email { get; }
}

[UsedImplicitly]
public abstract class UserValidator<T> : AbstractValidator<T> where T : IUserValidation
{
    protected UserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().SetValidator(new SharedValidations.Email());
    }
}