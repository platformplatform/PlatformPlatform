using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Authentication.Domain;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Authentication.TokenGeneration;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.Account.Features.Authentication.Commands;

[PublicAPI]
public sealed record RevokeSessionCommand : ICommand, IRequest<Result>
{
    [JsonIgnore]
    public SessionId Id { get; init; } = null!;
}

public sealed class RevokeSessionHandler(
    ISessionRepository sessionRepository,
    IUserRepository userRepository,
    IExecutionContext executionContext,
    ITelemetryEventsCollector events,
    TimeProvider timeProvider
) : IRequestHandler<RevokeSessionCommand, Result>
{
    public async Task<Result> Handle(RevokeSessionCommand command, CancellationToken cancellationToken)
    {
        var userEmail = executionContext.UserInfo.Email!;

        var session = await sessionRepository.GetByIdUnfilteredAsync(command.Id, cancellationToken);
        if (session is null)
        {
            return Result.NotFound($"Session with id '{command.Id}' not found.");
        }

        var sessionUser = await userRepository.GetByIdUnfilteredAsync(session.UserId, cancellationToken);
        if (sessionUser?.Email != userEmail)
        {
            return Result.Forbidden("You can only revoke your own sessions.");
        }

        if (session.IsRevoked)
        {
            return Result.BadRequest($"Session with id '{command.Id}' is already revoked.");
        }

        session.Revoke(timeProvider.GetUtcNow(), SessionRevokedReason.Revoked);
        sessionRepository.Update(session);

        events.CollectEvent(new SessionRevoked(SessionRevokedReason.Revoked));

        return Result.Success();
    }
}
