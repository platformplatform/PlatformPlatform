using Account.Features.FeatureFlags.Domain;
using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using SharedKernel.Domain;
using SharedKernel.FeatureFlags;

namespace Account.Features.FeatureFlags;

public sealed class PlanBasedFeatureFlagService(IFeatureFlagRepository featureFlagRepository, ITenantRepository tenantRepository, TimeProvider timeProvider)
{
    public async Task EvaluatePlanFlagsForTenantAsync(TenantId tenantId, SubscriptionPlan subscriptionPlan, CancellationToken cancellationToken)
    {
        var planTier = MapToPlanTier(subscriptionPlan);
        var planFlagDefinitions = SharedKernel.FeatureFlags.FeatureFlags.GetAll().Where(f => f.RequiredPlan is not null).ToArray();

        if (planFlagDefinitions.Length == 0) return;

        var existingOverrides = await featureFlagRepository.GetPlanBasedOverridesForTenantAsync(tenantId.Value, cancellationToken);
        var overridesByKey = existingOverrides.ToDictionary(f => f.FlagKey);
        var now = timeProvider.GetUtcNow();
        var changed = false;

        foreach (var definition in planFlagDefinitions)
        {
            var shouldBeEnabled = planTier >= definition.RequiredPlan!.Value;
            overridesByKey.TryGetValue(definition.Key, out var existingOverride);

            if (shouldBeEnabled)
            {
                if (existingOverride is null)
                {
                    var flag = FeatureFlag.CreateTenantOverride(definition.Key, tenantId.Value, FeatureFlagSource.Plan);
                    flag.Activate(now);
                    await featureFlagRepository.AddAsync(flag, cancellationToken);
                    changed = true;
                }
                else if (!IsActive(existingOverride))
                {
                    existingOverride.Activate(now);
                    featureFlagRepository.Update(existingOverride);
                    changed = true;
                }
            }
            else
            {
                if (existingOverride is not null && IsActive(existingOverride))
                {
                    existingOverride.Deactivate(now);
                    featureFlagRepository.Update(existingOverride);
                    changed = true;
                }
            }
        }

        if (changed)
        {
            var tenant = await tenantRepository.GetByIdUnfilteredAsync(tenantId, cancellationToken);
            if (tenant is not null)
            {
                tenant.IncrementFeatureFlagVersion();
                tenantRepository.Update(tenant);
            }
        }
    }

    private static bool IsActive(FeatureFlag flag)
    {
        return flag.EnabledAt is not null && (flag.DisabledAt is null || flag.EnabledAt > flag.DisabledAt);
    }

    private static PlanTier MapToPlanTier(SubscriptionPlan plan)
    {
        return plan switch
        {
            SubscriptionPlan.Basis => PlanTier.Free,
            SubscriptionPlan.Standard => PlanTier.Standard,
            SubscriptionPlan.Premium => PlanTier.Premium,
            _ => PlanTier.Free
        };
    }
}
