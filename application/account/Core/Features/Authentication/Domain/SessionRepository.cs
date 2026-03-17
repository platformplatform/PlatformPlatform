using System.Data.Common;
using Account.Database;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SharedKernel.Authentication.TokenGeneration;
using SharedKernel.Domain;
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
}

public sealed class SessionRepository(AccountDbContext accountDbContext, IServiceProvider serviceProvider)
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
