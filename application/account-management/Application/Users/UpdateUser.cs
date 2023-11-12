using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;

namespace PlatformPlatform.AccountManagement.Application.Users;

public sealed record UpdateUserCommand : ICommand, IUserValidation, IRequest<Result>
{
    [JsonIgnore] // Removes the Id from the API contract
    public UserId Id { get; init; } = null!;

    public required UserRole UserRole { get; init; }

    public required string Email { get; init; }
}

[UsedImplicitly]
public sealed class UpdateUserHandler : IRequestHandler<UpdateUserCommand, Result>
{
    private readonly IUserRepository _userRepository;

    public UpdateUserHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Result> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(command.Id, cancellationToken);
        if (user is null) return Result.NotFound($"User with id '{command.Id}' not found.");

        user.Update(command.Email, command.UserRole);
        _userRepository.Update(user);
        return Result.Success();
    }
}

[UsedImplicitly]
public sealed class UpdateUserValidator : UserValidator<UpdateUserCommand>
{
}