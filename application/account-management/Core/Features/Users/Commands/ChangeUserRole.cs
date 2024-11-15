using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.AccountManagement.TelemetryEvents;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.AccountManagement.Features.Users.Commands;

[PublicAPI]
public sealed record ChangeUserRoleCommand : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes this property from the API contract
    public UserId Id { get; init; } = null!;

    public required UserRole UserRole { get; init; }
}

public sealed class ChangeUserRoleHandler(IUserRepository userRepository, ITelemetryEventsCollector events)
    : IRequestHandler<ChangeUserRoleCommand, Result>
{
    public async Task<Result> Handle(ChangeUserRoleCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(command.Id, cancellationToken);
        if (user is null) return Result.NotFound($"User with id '{command.Id}' not found.");

        var oldUserRole = user.Role;
        user.ChangeUserRole(command.UserRole);
        userRepository.Update(user);

        events.CollectEvent(new UserRoleChanged(user.Id, oldUserRole, command.UserRole));

        return Result.Success();
    }
}
