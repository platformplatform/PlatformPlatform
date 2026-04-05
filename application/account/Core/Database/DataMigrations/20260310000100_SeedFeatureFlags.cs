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
        var featureFlags = FeatureFlags.GetAll();
        var now = DateTimeOffset.UtcNow;

        var seededCount = 0;
        foreach (var featureFlag in featureFlags)
        {
            if (featureFlag.Scope == FeatureFlagScope.System) continue;

            seededCount++;
            var id = FeatureFlagId.NewId().Value;

            var source = featureFlag.RequiredPlan is not null ? "Plan" : "Manual";

            await dbContext.Database.ExecuteSqlRawAsync(
                """
                INSERT INTO feature_flags (id, flag_key, tenant_id, user_id, created_at, modified_at, enabled_at, disabled_at, bucket_start, bucket_end, configurable_by_tenant, configurable_by_user, source)
                VALUES (@id, @flagKey, NULL, NULL, @now, NULL, NULL, NULL, NULL, NULL, @configurableByTenant, @configurableByUser, @source)
                ON CONFLICT (flag_key, tenant_id, user_id) DO UPDATE SET
                    configurable_by_tenant = @configurableByTenant,
                    configurable_by_user = @configurableByUser,
                    source = @source
                """,
                [
                    new NpgsqlParameter("@id", id),
                    new NpgsqlParameter("@flagKey", featureFlag.Key),
                    new NpgsqlParameter("@now", now),
                    new NpgsqlParameter("@configurableByTenant", featureFlag.ConfigurableByTenant),
                    new NpgsqlParameter("@configurableByUser", featureFlag.ConfigurableByUser),
                    new NpgsqlParameter("@source", source)
                ],
                cancellationToken
            );
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return $"Upserted {seededCount} feature featureFlag base rows";
    }
}
