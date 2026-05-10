using System.Data.Common;
using Account.Database;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SharedKernel.Authentication.TokenGeneration;
using SharedKernel.Domain;
using SharedKernel.EntityFramework;
using SharedKernel.Persistence;

namespace Account.Features.Authentication.Domain;

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

    /// <summary>
    ///     Attempts to refresh the session token if the current JTI and version match.
    ///     Returns false if another concurrent request already refreshed the session.
    /// </summary>
    Task<bool> TryRefreshAsync(SessionId sessionId, RefreshTokenJti currentJti, int currentVersion, RefreshTokenJti newJti, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>
    ///     Attempts to revoke the session for a replay attack without applying tenant query filters.
    ///     Uses atomic update to handle concurrent requests - only one will succeed, but all callers
    ///     can safely return ReplayAttackDetected since the session will be revoked either way.
    ///     This method should only be used during token refresh where tenant context comes from the token claims.
    /// </summary>
    Task<bool> TryRevokeForReplayUnfilteredAsync(SessionId sessionId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>
    ///     Returns the paged session history for a single user without applying tenant query filters. Used by the
    ///     back-office User detail page where tenant context is not established. Includes both active and revoked
    ///     sessions, ordered most-recent first.
    /// </summary>
    Task<(Session[] Sessions, int TotalItems, int TotalPages)> GetSessionsForUserUnfilteredAsync(UserId userId, int pageOffset, int pageSize, CancellationToken cancellationToken);

    /// <summary>
    ///     Returns the paged session history for any of the supplied user ids without applying tenant query filters.
    ///     Used by the back-office User detail page to surface sessions across every user record sharing the same email
    ///     across tenants. Includes active and revoked sessions, ordered most-recent first.
    /// </summary>
    Task<(Session[] Sessions, int TotalItems, int TotalPages)> GetSessionsForUsersUnfilteredAsync(UserId[] userIds, int pageOffset, int pageSize, CancellationToken cancellationToken);

    /// <summary>
    ///     Counts active (not revoked) sessions created at or after <paramref name="since" /> across all tenants
    ///     without applying tenant query filters. Used by the back-office dashboard KPI snapshot for active sessions
    ///     in the last 24 hours.
    /// </summary>
    Task<long> CountActiveSinceUnfilteredAsync(DateTimeOffset since, CancellationToken cancellationToken);
}

public sealed class SessionRepository(AccountDbContext accountDbContext, IServiceProvider serviceProvider)
    : RepositoryBase<Session, SessionId>(accountDbContext), ISessionRepository
{
    public async Task<Session?> GetByIdUnfilteredAsync(SessionId sessionId, CancellationToken cancellationToken)
    {
        return await DbSet.IgnoreQueryFilters().SingleOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
    }

    /// <summary>
    ///     Uses an atomic UPDATE via raw ADO.NET with a separate connection to ensure complete isolation.
    ///     This creates an independent database connection that commits immediately, preventing race conditions
    ///     when multiple concurrent requests attempt to refresh the same token.
    /// </summary>
    public async Task<bool> TryRefreshAsync(SessionId sessionId, RefreshTokenJti currentJti, int currentVersion, RefreshTokenJti newJti, DateTimeOffset now, CancellationToken cancellationToken)
    {
        // Create a new connection to ensure complete isolation from EF Core's transaction.
        // Use NpgsqlDataSource from DI to preserve the Entra ID token provider configured for Azure.
        // For SQLite (tests), fall back to creating a raw connection from the connection string.
        await using var connection = serviceProvider.GetService(typeof(NpgsqlDataSource)) is NpgsqlDataSource npgsqlDataSource
            ? await npgsqlDataSource.OpenConnectionAsync(cancellationToken)
            : await OpenFallbackConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
                              UPDATE sessions
                              SET previous_refresh_token_jti = refresh_token_jti,
                                  refresh_token_jti = @newJti,
                                  refresh_token_version = refresh_token_version + 1,
                                  modified_at = @now
                              WHERE id = @sessionId
                                AND refresh_token_jti = @currentJti
                                AND refresh_token_version = @currentVersion
                              """;

        var isSqlite = accountDbContext.Database.ProviderName is "Microsoft.EntityFrameworkCore.Sqlite";
        AddParameter(command, "@newJti", newJti.Value);
        AddParameter(command, "@now", isSqlite ? now.ToString("O") : now);
        AddParameter(command, "@sessionId", sessionId.Value);
        AddParameter(command, "@currentJti", currentJti.Value);
        AddParameter(command, "@currentVersion", currentVersion);

        var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);

        return rowsAffected == 1;
    }

    public async Task<bool> TryRevokeForReplayUnfilteredAsync(SessionId sessionId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var rowsAffected = await DbSet
            .IgnoreQueryFilters()
            .Where(s => s.Id == sessionId && s.RevokedAt == null)
            .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.RevokedAt, now)
                    .SetProperty(x => x.RevokedReason, SessionRevokedReason.ReplayAttackDetected)
                    .SetProperty(x => x.ModifiedAt, now),
                cancellationToken
            );

        return rowsAffected == 1;
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

    /// <summary>
    ///     Returns the paged session history for a single user without applying tenant query filters. Used by the
    ///     back-office User detail page where tenant context is not established. Includes both active and revoked
    ///     sessions, ordered most-recent first. SQLite cannot translate DateTimeOffset comparisons in ORDER BY, so
    ///     sessions are materialized and ordered in memory; a single user has very few sessions so scale is not a
    ///     concern.
    /// </summary>
    public async Task<(Session[] Sessions, int TotalItems, int TotalPages)> GetSessionsForUserUnfilteredAsync(UserId userId, int pageOffset, int pageSize, CancellationToken cancellationToken)
    {
        var sessions = await DbSet
            .IgnoreQueryFilters([QueryFilterNames.Tenant])
            .Where(s => s.UserId == userId)
            .ToArrayAsync(cancellationToken);

        var ordered = sessions.OrderByDescending(s => s.ModifiedAt ?? s.CreatedAt).ToArray();

        var totalItems = ordered.Length;
        var totalPages = totalItems == 0 ? 0 : (totalItems - 1) / pageSize + 1;
        var page = ordered.Skip(pageOffset * pageSize).Take(pageSize).ToArray();
        return (page, totalItems, totalPages);
    }

    /// <summary>
    ///     Returns the paged session history for any of the supplied user ids without applying tenant query filters.
    ///     Used by the back-office User detail page to surface sessions across every user record sharing the same email
    ///     across tenants. SQLite cannot translate DateTimeOffset comparisons in ORDER BY, so sessions are materialized
    ///     and ordered in memory; the cross-tenant set for one person is small enough that scale is not a concern.
    /// </summary>
    public async Task<(Session[] Sessions, int TotalItems, int TotalPages)> GetSessionsForUsersUnfilteredAsync(UserId[] userIds, int pageOffset, int pageSize, CancellationToken cancellationToken)
    {
        if (userIds.Length == 0) return ([], 0, 0);

        var sessions = await DbSet
            .IgnoreQueryFilters([QueryFilterNames.Tenant])
            .Where(s => userIds.AsEnumerable().Contains(s.UserId))
            .ToArrayAsync(cancellationToken);

        var ordered = sessions.OrderByDescending(s => s.ModifiedAt ?? s.CreatedAt).ToArray();

        var totalItems = ordered.Length;
        var totalPages = totalItems == 0 ? 0 : (totalItems - 1) / pageSize + 1;
        var page = ordered.Skip(pageOffset * pageSize).Take(pageSize).ToArray();
        return (page, totalItems, totalPages);
    }

    /// <summary>
    ///     Counts active (not revoked) sessions created at or after <paramref name="since" /> across all tenants
    ///     without applying tenant query filters. Used by the back-office dashboard KPI snapshot for active sessions
    ///     in the last 24 hours. SQLite cannot translate DateTimeOffset comparisons in WHERE, so sessions are
    ///     materialized and filtered in memory; the bounded 24-hour window keeps the set small.
    /// </summary>
    public async Task<long> CountActiveSinceUnfilteredAsync(DateTimeOffset since, CancellationToken cancellationToken)
    {
        var sessions = await DbSet
            .IgnoreQueryFilters([QueryFilterNames.Tenant])
            .Where(s => s.RevokedAt == null)
            .Select(s => new { s.CreatedAt })
            .ToArrayAsync(cancellationToken);
        return sessions.LongCount(s => s.CreatedAt >= since);
    }

    private async Task<DbConnection> OpenFallbackConnectionAsync(CancellationToken cancellationToken)
    {
        var existingConnection = accountDbContext.Database.GetDbConnection();
        var connection = (DbConnection)Activator.CreateInstance(existingConnection.GetType())!;
        connection.ConnectionString = accountDbContext.Database.GetConnectionString();
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
