using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace SharedKernel.Database;

public sealed class DataMigrationRunner<TContext>(TContext dbContext, IServiceProvider serviceProvider, ILogger<DataMigrationRunner<TContext>> logger)
    where TContext : DbContext
{
    private static readonly long LockKey = typeof(TContext).FullName!.GetHashCode();

    public async Task RunMigrationsAsync(CancellationToken cancellationToken)
    {
        var dataMigrations = DiscoverDataMigrations();
        if (dataMigrations.Count == 0)
        {
            return;
        }

        logger.LogInformation("Acquiring an exclusive lock for data migration application. This may take a while if data migrations are already being applied");

        await using var connection = (NpgsqlConnection)dbContext.Database.GetDbConnection();
        await connection.OpenAsync(cancellationToken);

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
            await using var releaseLockCommand = connection.CreateCommand();
            releaseLockCommand.CommandText = "SELECT pg_advisory_unlock(@key)";
            releaseLockCommand.Parameters.AddWithValue("key", LockKey);
            await releaseLockCommand.ExecuteNonQueryAsync(cancellationToken);
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
        logger.LogInformation("Executing data migration: '{MigrationId}'", dataMigration.Id);

        var stopwatch = Stopwatch.StartNew();

        var executionStrategy = dbContext.Database.CreateExecutionStrategy();
        await executionStrategy.ExecuteAsync(async () =>
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

                try
                {
                    var summary = await dataMigration.ExecuteAsync(cancellationToken);

                    if (dbContext.ChangeTracker.HasChanges())
                    {
                        throw new InvalidOperationException($"Data migration '{dataMigration.Id}' has unsaved changes. Ensure you call dbContext.SaveChangesAsync() before returning from ExecuteAsync().");
                    }

                    await RecordDataMigrationAsync(dataMigration.Id, stopwatch.ElapsedMilliseconds, summary, cancellationToken);

                    await transaction.CommitAsync(cancellationToken);

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
                    await transaction.RollbackAsync(cancellationToken);
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
                new NpgsqlParameter("@ExecutedAt", DateTimeOffset.UtcNow),
                new NpgsqlParameter("@ExecutionTimeMs", elapsedMs),
                new NpgsqlParameter("@Summary", summary)
            ],
            cancellationToken
        );
    }
}
