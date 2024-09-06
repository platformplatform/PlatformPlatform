using FluentValidation;
using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Core.TelemetryEvents;
using PlatformPlatform.AccountManagement.Core.Users.Domain;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.TelemetryEvents;
using PlatformPlatform.SharedKernel.Validation;

namespace PlatformPlatform.AccountManagement.Core.Users.Commands;

[PublicAPI]
public sealed record UpdateUserCommand : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes the Id from the API contract
    public UserId Id { get; init; } = null!;

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
        RuleFor(x => x.FirstName).MaximumLength(30).WithMessage("First name must be no longer than 30 characters.");
        RuleFor(x => x.LastName).MaximumLength(30).WithMessage("Last name must be no longer than 30 characters.");
        RuleFor(x => x.Title).MaximumLength(50).WithMessage("Title must be no longer than 50 characters.");
    }
}

public sealed class UpdateUserHandler(IUserRepository userRepository, ITelemetryEventsCollector events)
    : IRequestHandler<UpdateUserCommand, Result>
{
    public async Task<Result> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.Id, cancellationToken);
        if (user is null) return Result.NotFound($"User with id '{command.Id}' not found.");

        user.UpdateEmail(command.Email);
        user.Update(command.FirstName, command.LastName, command.Title);
        userRepository.Update(user);

        events.CollectEvent(new UserUpdated());

        return Result.Success();
    }
}
