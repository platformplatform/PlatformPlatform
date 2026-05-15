using Account.Features.Users.Domain;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;
using SharedKernel.FeatureFlags;
using SharedKernel.Telemetry;

namespace Account.Features.Users.BackOffice.Commands;

[PublicAPI]
public sealed record SetUserAbInclusionPinCommand : ICommand, IRequest<Result>
{
    [JsonIgnore] // Removes from API contract
    public UserId UserId { get; init; } = null!;

    public AbInclusionPin? AbInclusionPin { get; init; }
}

public sealed class SetUserAbInclusionPinHandler(IUserRepository userRepository, ITelemetryEventsCollector events)
    : IRequestHandler<SetUserAbInclusionPinCommand, Result>
{
    public async Task<Result> Handle(SetUserAbInclusionPinCommand command, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdUnfilteredAsync(command.UserId, cancellationToken);
        if (user is null) return Result.NotFound($"User with id '{command.UserId}' not found.");

        var fromPin = user.AbInclusionPin;
        user.SetAbInclusionPin(command.AbInclusionPin);
        userRepository.Update(user);

        events.CollectEvent(new UserAbInclusionPinUpdated(user.Id, fromPin, command.AbInclusionPin));

        return Result.Success();
    }
}
