using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Features.Authentication.Domain;
using PlatformPlatform.SharedKernel.Authentication.TokenGeneration;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;

namespace PlatformPlatform.AccountManagement.Features.Authentication.Queries;

[PublicAPI]
public sealed record GetUserSessionsQuery : IRequest<Result<UserSessionsResponse>>;

[PublicAPI]
public sealed record UserSessionsResponse(UserSessionInfo[] Sessions);

[PublicAPI]
public sealed record UserSessionInfo(
    SessionId Id,
    DateTimeOffset CreatedAt,
    DeviceType DeviceType,
    string UserAgent,
    string IpAddress,
    DateTimeOffset LastActivityAt,
    bool IsCurrent
);

public sealed class GetUserSessionsHandler(ISessionRepository sessionRepository, IExecutionContext executionContext)
    : IRequestHandler<GetUserSessionsQuery, Result<UserSessionsResponse>>
{
    public async Task<Result<UserSessionsResponse>> Handle(GetUserSessionsQuery query, CancellationToken cancellationToken)
    {
        var userId = executionContext.UserInfo.Id!;
        var currentSessionId = executionContext.UserInfo.SessionId;

        var sessions = await sessionRepository.GetActiveSessionsForUserAsync(userId, cancellationToken);

        var sessionInfos = sessions.Select(s => new UserSessionInfo(
                s.Id,
                s.CreatedAt,
                s.DeviceType,
                s.UserAgent,
                s.IpAddress,
                s.ModifiedAt ?? s.CreatedAt,
                currentSessionId is not null && s.Id == currentSessionId
            )
        ).ToArray();

        return new UserSessionsResponse(sessionInfos);
    }
}
