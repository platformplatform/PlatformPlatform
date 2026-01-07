using Microsoft.EntityFrameworkCore;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.SharedKernel.Authentication.TokenGeneration;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Persistence;

namespace PlatformPlatform.AccountManagement.Features.Authentication.Domain;

public interface ISessionRepository : ICrudRepository<Session, SessionId>
{
    /// <summary>
    ///     Retrieves a session by ID without applying tenant query filters.
    ///     This method should only be used during token refresh where tenant context comes from the token claims.
    /// </summary>
    Task<Session?> GetByIdUnfilteredAsync(SessionId sessionId, CancellationToken cancellationToken);

    Task<Session[]> GetActiveSessionsForUserAsync(UserId userId, CancellationToken cancellationToken);

    /// <summary>
    ///     Retrieves all active sessions for multiple users across all tenants without applying query filters.
    ///     This method should only be used in the Sessions dialog where users need to see all sessions for their email.
    /// </summary>
    Task<Session[]> GetActiveSessionsForUsersUnfilteredAsync(UserId[] userIds, CancellationToken cancellationToken);
}

public sealed class SessionRepository(AccountManagementDbContext accountManagementDbContext)
    : RepositoryBase<Session, SessionId>(accountManagementDbContext), ISessionRepository
{
    public async Task<Session?> GetByIdUnfilteredAsync(SessionId sessionId, CancellationToken cancellationToken)
    {
        return await DbSet.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
    }

    public async Task<Session[]> GetActiveSessionsForUserAsync(UserId userId, CancellationToken cancellationToken)
    {
        var sessions = await DbSet
            .Where(s => s.UserId == userId && s.RevokedAt == null)
            .ToArrayAsync(cancellationToken);
        return sessions.OrderByDescending(s => s.ModifiedAt ?? s.CreatedAt).ToArray();
    }

    public async Task<Session[]> GetActiveSessionsForUsersUnfilteredAsync(UserId[] userIds, CancellationToken cancellationToken)
    {
        var sessions = await DbSet
            .IgnoreQueryFilters()
            .Where(s => userIds.AsEnumerable().Contains(s.UserId) && s.RevokedAt == null)
            .ToArrayAsync(cancellationToken);
        return sessions.OrderByDescending(s => s.ModifiedAt ?? s.CreatedAt).ToArray();
    }
}
