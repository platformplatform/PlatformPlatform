using FluentValidation;
using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Telemetry;
using PlatformPlatform.SharedKernel.Validation;

namespace PlatformPlatform.AccountManagement.Features.Users.Commands;

[PublicAPI]
public sealed record UpdateUserCommand : ICommand, IRequest<Result>
{
    public required string Email { get; init; }

    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    public required string Title { get; init; }
}

public sealed class UpdateUserValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().SetValidator(new SharedValidations.Email());
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(30).WithMessage("First name must be no longer than 30 characters.");
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(30).WithMessage("Last name must be no longer than 30 characters.");
        RuleFor(x => x.Title).MaximumLength(50).WithMessage("Title must be no longer than 50 characters.");
    }
}

public sealed class UpdateUserHandler(IUserRepository userRepository, ITelemetryEventsCollector events)
    : IRequestHandler<UpdateUserCommand, Result>
{
    public async Task<Result> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetLoggedInUserAsync(cancellationToken);
        if (user is null) return Result.BadRequest("User not found.");

        user.UpdateEmail(command.Email);
        user.Update(command.FirstName, command.LastName, command.Title);
        userRepository.Update(user);

        events.CollectEvent(new UserUpdated());

        return Result.Success();
    }
}
