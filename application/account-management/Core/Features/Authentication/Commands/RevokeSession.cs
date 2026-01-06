using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Features.Authentication.Domain;
using PlatformPlatform.SharedKernel.Authentication.TokenGeneration;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.AccountManagement.Features.Authentication.Commands;

[PublicAPI]
public sealed record RevokeSessionCommand : ICommand, IRequest<Result>
{
    [JsonIgnore]
    public SessionId Id { get; init; } = null!;
}

public sealed class RevokeSessionHandler(
    ISessionRepository sessionRepository,
    IExecutionContext executionContext,
    ITelemetryEventsCollector events,
    TimeProvider timeProvider
) : IRequestHandler<RevokeSessionCommand, Result>
{
    public async Task<Result> Handle(RevokeSessionCommand command, CancellationToken cancellationToken)
    {
        var userId = executionContext.UserInfo.Id!;

        var session = await sessionRepository.GetByIdAsync(command.Id, cancellationToken);
        if (session is null)
        {
            return Result.NotFound($"Session with id '{command.Id}' not found.");
        }

        if (session.UserId != userId)
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
