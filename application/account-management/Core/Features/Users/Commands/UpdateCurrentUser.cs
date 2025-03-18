using FluentValidation;
using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Telemetry;
using PlatformPlatform.SharedKernel.Validation;

namespace PlatformPlatform.AccountManagement.Features.Users.Commands;

[PublicAPI]
public sealed record UpdateCurrentUserCommand(string Email, string FirstName, string LastName, string Title)
    : ICommand, IRequest<Result>
{
    public string Email { get; } = Email.ToLower().Trim();

    public string FirstName { get; } = FirstName.Trim();

    public string LastName { get; } = LastName.Trim();

    public string Title { get; } = Title.Trim();
}

public sealed class UpdateCurrentUserValidator : AbstractValidator<UpdateCurrentUserCommand>
{
    public UpdateCurrentUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().SetValidator(new SharedValidations.Email());
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(30).WithMessage("First name must be no longer than 30 characters.");
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(30).WithMessage("Last name must be no longer than 30 characters.");
        RuleFor(x => x.Title).MaximumLength(50).WithMessage("Title must be no longer than 50 characters.");
    }
}

public sealed class UpdateCurrentUserHandler(IUserRepository userRepository, ITelemetryEventsCollector events)
    : IRequestHandler<UpdateCurrentUserCommand, Result>
{
    public async Task<Result> Handle(UpdateCurrentUserCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetLoggedInUserAsync(cancellationToken);

        user.UpdateEmail(command.Email);
        user.Update(command.FirstName, command.LastName, command.Title);
        userRepository.Update(user);

        events.CollectEvent(new UserUpdated());

        return Result.Success();
    }
}
