using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace SharedKernel.Database;

public sealed class DataMigrationRunner<TContext>(TContext dbContext, IServiceProvider serviceProvider, ILogger<DataMigrationRunner<TContext>> logger)
    where TContext : DbContext
{
    private static readonly long LockKey = BitConverter.ToInt64(SHA256.HashData(Encoding.UTF8.GetBytes(typeof(TContext).FullName!)));

    public async Task RunMigrationsAsync(CancellationToken cancellationToken)
    {
        var dataMigrations = DiscoverDataMigrations();
        if (dataMigrations.Count == 0)
        {
            return;
        }

        logger.LogInformation("Acquiring an exclusive lock for data migration application. This may take a while if data migrations are already being applied");

        await using var connection = serviceProvider.GetService(typeof(NpgsqlDataSource)) is NpgsqlDataSource npgsqlDataSource
            ? await npgsqlDataSource.OpenConnectionAsync(cancellationToken)
            : (NpgsqlConnection)dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open) await connection.OpenAsync(cancellationToken);

        await using var lockCommand = connection.CreateCommand();
        lockCommand.CommandText = "SELECT pg_advisory_lock(@key)";
        lockCommand.Parameters.AddWithValue("key", LockKey);
        await lockCommand.ExecuteNonQueryAsync(cancellationToken);

        try
        {
            await EnsureDataMigrationHistoryTableExistsAsync(cancellationToken);

            var executedDataMigrations = await GetExecutedDataMigrationsAsync(cancellationToken);

            var pendingDataMigrations = dataMigrations
                .Where(dm => !executedDataMigrations.Contains(dm.Id))
                .OrderBy(dm => dm.Id)
                .ToList();

            if (pendingDataMigrations.Count == 0)
            {
                logger.LogInformation("All data migrations have already been applied");
                return;
            }

            logger.LogInformation("Starting data migrations - found {Count} pending data migrations", pendingDataMigrations.Count);

            foreach (var dataMigration in pendingDataMigrations)
            {
                await ExecuteMigrationAsync(dataMigration, cancellationToken);
            }

            logger.LogInformation("Completed all data migrations successfully");
        }
        finally
        {
            try
            {
                await using var releaseLockCommand = connection.CreateCommand();
                releaseLockCommand.CommandText = "SELECT pg_advisory_unlock(@key)";
                releaseLockCommand.Parameters.AddWithValue("key", LockKey);
                var unlocked = (bool)(await releaseLockCommand.ExecuteScalarAsync(CancellationToken.None))!;
                if (!unlocked)
                {
                    logger.LogWarning("Advisory lock {LockKey} was not held when attempting to release", LockKey);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to release advisory lock {LockKey}", LockKey);
            }
        }
    }

    private List<IDataMigration> DiscoverDataMigrations()
    {
        var migrations = typeof(TContext).Assembly.GetTypes()
            .Where(t => typeof(IDataMigration).IsAssignableFrom(t) && t is { IsClass: true, IsAbstract: false })
            .Select(t => (IDataMigration)ActivatorUtilities.CreateInstance(serviceProvider, t))
            .ToList();

        foreach (var migration in migrations)
        {
            if (!Regex.IsMatch(migration.Id, @"^\d{14}_\w+$"))
            {
                throw new InvalidOperationException($"Data migration ID '{migration.Id}' must follow format 'YYYYMMDDHHmmss_ClassName' (14 digits, underscore, class name)");
            }

            var expectedClassName = migration.Id.Substring(15);
            var actualClassName = migration.GetType().Name;
            if (!actualClassName.Equals(expectedClassName, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Data migration class name '{actualClassName}' must match ID suffix '{expectedClassName}'");
            }

            if (migration.Timeout <= TimeSpan.Zero || migration.Timeout > TimeSpan.FromMinutes(20))
            {
                throw new InvalidOperationException($"Data migration '{migration.Id}' timeout {migration.Timeout} must be between 1 second and 20 minutes.");
            }
        }

        return migrations;
    }

    private async Task EnsureDataMigrationHistoryTableExistsAsync(CancellationToken cancellationToken)
    {
        var sql = """
                  DO $$
                  BEGIN
                      IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = '__DataMigrationsHistory') THEN
                          ALTER TABLE "__DataMigrationsHistory" RENAME TO __data_migrations_history;
                          ALTER TABLE __data_migrations_history RENAME COLUMN "MigrationId" TO migration_id;
                          ALTER TABLE __data_migrations_history RENAME COLUMN "ProductVersion" TO product_version;
                          ALTER TABLE __data_migrations_history RENAME COLUMN "ExecutedAt" TO executed_at;
                          ALTER TABLE __data_migrations_history RENAME COLUMN "ExecutionTimeMs" TO execution_time_ms;
                          ALTER TABLE __data_migrations_history RENAME COLUMN "Summary" TO summary;
                          ALTER TABLE __data_migrations_history RENAME CONSTRAINT "PK___DataMigrationsHistory" TO pk___data_migrations_history;
                      END IF;
                  END $$;

                  CREATE TABLE IF NOT EXISTS __data_migrations_history (
                      migration_id text NOT NULL,
                      product_version text NOT NULL,
                      executed_at timestamptz NOT NULL,
                      execution_time_ms bigint NOT NULL,
                      summary text NOT NULL,
                      CONSTRAINT pk___data_migrations_history PRIMARY KEY (migration_id)
                  );
                  """;

        await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    private async Task<HashSet<string>> GetExecutedDataMigrationsAsync(CancellationToken cancellationToken)
    {
        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = "SELECT migration_id FROM __data_migrations_history";
        command.Transaction = dbContext.Database.CurrentTransaction?.GetDbTransaction();

        var executedDataMigrations = new HashSet<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            executedDataMigrations.Add(reader.GetString(0));
        }

        return executedDataMigrations;
    }

    private async Task ExecuteMigrationAsync(IDataMigration dataMigration, CancellationToken cancellationToken)
    {
        logger.LogInformation("Executing data migration: '{MigrationId}' with timeout {Timeout}", dataMigration.Id, dataMigration.Timeout);

        var stopwatch = Stopwatch.StartNew();
        using var timeoutCancellationTokenSource = new CancellationTokenSource(dataMigration.Timeout);
        using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCancellationTokenSource.Token);
        var linkedCancellationToken = linkedCancellationTokenSource.Token;

        if (dataMigration.ManagesOwnTransactions)
        {
            var summary = await dataMigration.ExecuteAsync(linkedCancellationToken);

            await using var historyTransaction = await dbContext.Database.BeginTransactionAsync(linkedCancellationToken);
            await RecordDataMigrationAsync(dataMigration.Id, stopwatch.ElapsedMilliseconds, summary, linkedCancellationToken);
            await historyTransaction.CommitAsync(linkedCancellationToken);

            logger.LogInformation(
                "Completed data migration: '{MigrationId}' in {ElapsedMs}ms - {Summary}",
                dataMigration.Id,
                stopwatch.ElapsedMilliseconds,
                summary
            );
            return;
        }

        var executionStrategy = dbContext.Database.CreateExecutionStrategy();
        await executionStrategy.ExecuteAsync(async () =>
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(linkedCancellationToken);

                try
                {
                    var summary = await dataMigration.ExecuteAsync(linkedCancellationToken);

                    if (dbContext.ChangeTracker.HasChanges())
                    {
                        throw new InvalidOperationException($"Data migration '{dataMigration.Id}' has unsaved changes. Ensure you call dbContext.SaveChangesAsync() before returning from ExecuteAsync().");
                    }

                    await RecordDataMigrationAsync(dataMigration.Id, stopwatch.ElapsedMilliseconds, summary, linkedCancellationToken);

                    await transaction.CommitAsync(linkedCancellationToken);

                    logger.LogInformation(
                        "Completed data migration: '{MigrationId}' in {ElapsedMs}ms - {Summary}",
                        dataMigration.Id,
                        stopwatch.ElapsedMilliseconds,
                        summary
                    );
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to execute data migration: '{MigrationId}'", dataMigration.Id);
                    await transaction.RollbackAsync(CancellationToken.None);
                    throw;
                }
            }
        );
    }

    private async Task RecordDataMigrationAsync(string migrationId, long elapsedMs, string summary, CancellationToken cancellationToken)
    {
        var productVersion = typeof(TContext).Assembly.GetName().Version!.ToString();

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO __data_migrations_history (migration_id, product_version, executed_at, execution_time_ms, summary)
            VALUES (@MigrationId, @ProductVersion, @ExecutedAt, @ExecutionTimeMs, @Summary);
            """,
            [
                new NpgsqlParameter("@MigrationId", migrationId),
                new NpgsqlParameter("@ProductVersion", productVersion),
                new NpgsqlParameter("@ExecutedAt", serviceProvider.GetRequiredService<TimeProvider>().GetUtcNow()),
                new NpgsqlParameter("@ExecutionTimeMs", elapsedMs),
                new NpgsqlParameter("@Summary", summary)
            ],
            cancellationToken
        );
    }
}
