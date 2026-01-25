using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using PlatformPlatform.Account.Database;
using PlatformPlatform.SharedKernel.Authentication.TokenGeneration;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Persistence;

namespace PlatformPlatform.Account.Features.Authentication.Domain;

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
}

public sealed class SessionRepository(AccountDbContext accountDbContext)
    : RepositoryBase<Session, SessionId>(accountDbContext), ISessionRepository
{
    public async Task<Session?> GetByIdUnfilteredAsync(SessionId sessionId, CancellationToken cancellationToken)
    {
        return await DbSet.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
    }

    /// <summary>
    ///     Uses an atomic UPDATE via raw ADO.NET with a separate connection to ensure complete isolation.
    ///     This creates an independent database connection that commits immediately, preventing race conditions
    ///     when multiple concurrent requests attempt to refresh the same token.
    /// </summary>
    public async Task<bool> TryRefreshAsync(SessionId sessionId, RefreshTokenJti currentJti, int currentVersion, RefreshTokenJti newJti, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var existingConnection = accountDbContext.Database.GetDbConnection();

        // Create a new connection of the same type to ensure complete isolation from EF Core's transaction.
        await using var connection = (DbConnection)Activator.CreateInstance(existingConnection.GetType())!;
        connection.ConnectionString = accountDbContext.Database.GetConnectionString();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
                              UPDATE Sessions
                              SET PreviousRefreshTokenJti = RefreshTokenJti,
                                  RefreshTokenJti = @newJti,
                                  RefreshTokenVersion = RefreshTokenVersion + 1,
                                  ModifiedAt = @now
                              WHERE Id = @sessionId
                                AND RefreshTokenJti = @currentJti
                                AND RefreshTokenVersion = @currentVersion
                              """;

        AddParameter(command, "@newJti", newJti.Value);
        AddParameter(command, "@now", now.ToString("O"));
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

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
