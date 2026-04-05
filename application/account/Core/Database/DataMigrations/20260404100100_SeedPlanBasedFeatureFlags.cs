using Account.Features.FeatureFlags.Domain;
using Account.Features.Subscriptions.Domain;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using SharedKernel.Database;
using SharedKernel.FeatureFlags;

namespace Account.Database.DataMigrations;

public sealed class SeedPlanBasedFeatureFlags(AccountDbContext dbContext) : IDataMigration
{
    public string Id => "20260404100100_SeedPlanBasedFeatureFlags";

    public TimeSpan Timeout { get; } = TimeSpan.FromMinutes(5);

    public async Task<string> ExecuteAsync(CancellationToken cancellationToken)
    {
        var planFeatureFlags = FeatureFlags.GetAll().Where(f => f.RequiredPlan is not null).ToArray();
        if (planFeatureFlags.Length == 0) return "No plan-based feature flags defined";

        var planMapping = new Dictionary<string, PlanTier>
        {
            [nameof(SubscriptionPlan.Basis)] = PlanTier.Free,
            [nameof(SubscriptionPlan.Standard)] = PlanTier.Standard,
            [nameof(SubscriptionPlan.Premium)] = PlanTier.Premium
        };

        var tenants = await dbContext.Database.SqlQueryRaw<TenantSubscriptionInfo>(
            """
            SELECT t.id AS tenant_id, s.plan AS plan
            FROM tenants t
            JOIN subscriptions s ON s.tenant_id = t.id
            WHERE t.deleted_at IS NULL
            """
        ).ToArrayAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var seededCount = 0;

        foreach (var tenant in tenants)
        {
            if (!planMapping.TryGetValue(tenant.Plan, out var tenantSubscriptionPlan)) continue;

            foreach (var featureFlag in planFeatureFlags)
            {
                var shouldBeEnabled = tenantSubscriptionPlan >= featureFlag.RequiredPlan!.Value;
                var id = FeatureFlagId.NewId().Value;

                await dbContext.Database.ExecuteSqlRawAsync(
                    """
                    INSERT INTO feature_flags (id, flag_key, tenant_id, user_id, created_at, modified_at, enabled_at, disabled_at, bucket_start, bucket_end, configurable_by_tenant, configurable_by_user, source)
                    VALUES (@id, @flagKey, @tenantId, NULL, @now, NULL, @enabledAt, @disabledAt, NULL, NULL, false, false, 'Plan')
                    ON CONFLICT (flag_key, tenant_id, user_id) DO UPDATE SET
                        enabled_at = CASE WHEN feature_flags.source = 'Plan' THEN @enabledAt ELSE feature_flags.enabled_at END,
                        disabled_at = CASE WHEN feature_flags.source = 'Plan' THEN @disabledAt ELSE feature_flags.disabled_at END,
                        source = CASE WHEN feature_flags.source = 'Manual' THEN 'Plan' ELSE feature_flags.source END
                    """,
                    [
                        new NpgsqlParameter("@id", id),
                        new NpgsqlParameter("@flagKey", featureFlag.Key),
                        new NpgsqlParameter("@tenantId", tenant.TenantId),
                        new NpgsqlParameter("@now", now),
                        new NpgsqlParameter("@enabledAt", shouldBeEnabled ? now : DBNull.Value),
                        new NpgsqlParameter("@disabledAt", shouldBeEnabled ? DBNull.Value : now)
                    ],
                    cancellationToken
                );

                seededCount++;
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return $"Seeded {seededCount} plan-based feature featureFlag overrides across {tenants.Length} tenants";
    }

    [UsedImplicitly]
    private sealed record TenantSubscriptionInfo(long TenantId, string Plan);
}
