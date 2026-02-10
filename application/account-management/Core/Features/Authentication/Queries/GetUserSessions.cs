using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Features.Authentication.Domain;
using PlatformPlatform.AccountManagement.Features.Tenants.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
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
    LoginMethod LoginMethod,
    DeviceType DeviceType,
    string UserAgent,
    string IpAddress,
    DateTimeOffset LastActivityAt,
    bool IsCurrent,
    string TenantName
);

public sealed class GetUserSessionsHandler(
    ISessionRepository sessionRepository,
    IUserRepository userRepository,
    ITenantRepository tenantRepository,
    IExecutionContext executionContext
) : IRequestHandler<GetUserSessionsQuery, Result<UserSessionsResponse>>
{
    public async Task<Result<UserSessionsResponse>> Handle(GetUserSessionsQuery query, CancellationToken cancellationToken)
    {
        var userEmail = executionContext.UserInfo.Email!;
        var currentSessionId = executionContext.UserInfo.SessionId;

        var users = await userRepository.GetUsersByEmailUnfilteredAsync(userEmail, cancellationToken);
        var userIds = users.Select(u => u.Id).ToArray();

        var sessions = await sessionRepository.GetActiveSessionsForUsersUnfilteredAsync(userIds, cancellationToken);

        var tenantIds = sessions.Select(s => s.TenantId).Distinct().ToArray();
        var tenants = await tenantRepository.GetByIdsAsync(tenantIds, cancellationToken);
        var tenantLookup = tenants.ToDictionary(t => t.Id, t => t.Name);

        var sessionInfos = sessions.Select(s => new UserSessionInfo(
                s.Id,
                s.CreatedAt,
                s.LoginMethod,
                s.DeviceType,
                s.UserAgent,
                s.IpAddress,
                s.ModifiedAt ?? s.CreatedAt,
                currentSessionId is not null && s.Id == currentSessionId,
                tenantLookup.GetValueOrDefault(s.TenantId) ?? string.Empty
            )
        ).ToArray();

        return new UserSessionsResponse(sessionInfos);
    }
}
