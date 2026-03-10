using Account.Features.FeatureFlags.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SharedKernel.Database;
using SharedKernel.FeatureFlags;

namespace Account.Database.DataMigrations;

public sealed class SeedFeatureFlags(AccountDbContext dbContext) : IDataMigration
{
    public string Id => "20260310000100_SeedFeatureFlags";

    public TimeSpan Timeout { get; } = TimeSpan.FromMinutes(1);

    public async Task<string> ExecuteAsync(CancellationToken cancellationToken)
    {
        var flags = FeatureFlags.GetAll();
        var now = DateTimeOffset.UtcNow;

        foreach (var flag in flags)
        {
            var id = FeatureFlagId.NewId().Value;

            await dbContext.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO feature_flags (id, flag_key, tenant_id, user_id, created_at, modified_at, enabled_at, disabled_at, bucket_start, bucket_end, configurable_by_tenant, configurable_by_user)
                VALUES (@id, @flagKey, NULL, NULL, @now, NULL, NULL, NULL, NULL, NULL, @configurableByTenant, @configurableByUser)
                ON CONFLICT (flag_key, tenant_id, user_id) DO UPDATE SET
                    configurable_by_tenant = @configurableByTenant,
                    configurable_by_user = @configurableByUser
                """,
                [
                    new NpgsqlParameter("@id", id),
                    new NpgsqlParameter("@flagKey", flag.Key),
                    new NpgsqlParameter("@now", now),
                    new NpgsqlParameter("@configurableByTenant", flag.ConfigurableByTenant),
                    new NpgsqlParameter("@configurableByUser", flag.ConfigurableByUser)
                ],
                cancellationToken
            );
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return $"Upserted {flags.Length} feature flag base rows";
    }
}
