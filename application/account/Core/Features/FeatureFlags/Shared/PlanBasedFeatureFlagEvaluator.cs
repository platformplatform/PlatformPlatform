using Account.Database;
using Account.Features.FeatureFlags.Domain;
using Account.Features.Subscriptions.Domain;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
using SharedKernel.FeatureFlags;
using SharedKernel.Telemetry;

namespace Account.Features.FeatureFlags.Shared;

public sealed class PlanBasedFeatureFlagEvaluator(
    IFeatureFlagRepository featureFlagRepository,
    AccountDbContext accountDbContext,
    TimeProvider timeProvider,
    ITelemetryEventsCollector telemetryEventsCollector
)
{
    public async Task EvaluatePlanFlagsForTenantAsync(TenantId tenantId, SubscriptionPlan subscriptionPlan, CancellationToken cancellationToken)
    {
        var subscriptionPlanTier = MapToPlanTier(subscriptionPlan);
        var planFeatureFlagDefinitions = SharedKernel.FeatureFlags.FeatureFlags.GetAll().Where(f => f.RequiredPlan is not null).ToArray();

        if (planFeatureFlagDefinitions.Length == 0) return;

        // Serialize the read-then-insert window per tenant. `pg_advisory_xact_lock` auto-releases at
        // commit, so the read-existing-overrides → conditional-insert pair is atomic per tenant.
        // Required because this evaluator runs on every JWT refresh / login, and concurrent fan-in for
        // the same tenant otherwise hits the unique index as a 500. The lock is PostgreSQL-specific;
        // in-memory SQLite test runs cannot exhibit cross-process concurrency, so we skip it there.
        if (accountDbContext.Database.ProviderName is not "Microsoft.EntityFrameworkCore.Sqlite")
        {
            await accountDbContext.Database.ExecuteSqlAsync(
                $"SELECT pg_advisory_xact_lock(hashtextextended('plan_flags:' || {tenantId.Value}, 0))",
                cancellationToken
            );
        }

        var existingOverrides = await featureFlagRepository.GetPlanBasedOverridesForTenantAsync(tenantId, cancellationToken);
        var overridesByKey = existingOverrides.ToDictionary(f => f.FlagKey);
        var now = timeProvider.GetUtcNow();

        foreach (var definition in planFeatureFlagDefinitions)
        {
            var shouldBeEnabled = subscriptionPlanTier >= definition.RequiredPlan!.Value;
            overridesByKey.TryGetValue(definition.Key, out var existingOverride);

            if (shouldBeEnabled)
            {
                if (existingOverride is null)
                {
                    var featureFlag = FeatureFlag.CreateTenantOverride(definition.Key, tenantId, definition.Scope, FeatureFlagSource.Plan);
                    featureFlag.Activate(now);
                    await featureFlagRepository.AddAsync(featureFlag, cancellationToken);
                    telemetryEventsCollector.CollectEvent(new FeatureFlagPlanOverrideActivated(definition.Key, tenantId, subscriptionPlanTier));
                }
                else if (!existingOverride.IsActive)
                {
                    existingOverride.Activate(now);
                    featureFlagRepository.Update(existingOverride);
                    telemetryEventsCollector.CollectEvent(new FeatureFlagPlanOverrideActivated(definition.Key, tenantId, subscriptionPlanTier));
                }
            }
            else
            {
                if (existingOverride?.IsActive == true)
                {
                    existingOverride.Deactivate(now);
                    featureFlagRepository.Update(existingOverride);
                    telemetryEventsCollector.CollectEvent(new FeatureFlagPlanOverrideDeactivated(definition.Key, tenantId, subscriptionPlanTier));
                }
            }
        }

        // Commit so the subsequent FeatureFlagEvaluator.EvaluateAsync read (typically the very next
        // call in UserInfoFactory) sees the rows this method just inserted. Without this commit, the
        // EF query reads pre-insert state and the JWT ships missing the newly-eligible plan flags
        // until the next refresh. The advisory_xact_lock acquired above releases here on commit.
        await accountDbContext.SaveChangesAsync(cancellationToken);
    }

    private static PlanTier MapToPlanTier(SubscriptionPlan plan)
    {
        return plan switch
        {
            SubscriptionPlan.Basis => PlanTier.Free,
            SubscriptionPlan.Standard => PlanTier.Standard,
            SubscriptionPlan.Premium => PlanTier.Premium,
            _ => throw new UnreachableException($"Unknown subscription plan '{plan}'.")
        };
    }
}
