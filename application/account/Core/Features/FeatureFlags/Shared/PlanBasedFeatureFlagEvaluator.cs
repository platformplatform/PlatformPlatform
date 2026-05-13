using Account.Features.FeatureFlags.Domain;
using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using SharedKernel.Domain;
using SharedKernel.FeatureFlags;

namespace Account.Features.FeatureFlags.Shared;

public sealed class PlanBasedFeatureFlagEvaluator(IFeatureFlagRepository featureFlagRepository, ITenantRepository tenantRepository, TimeProvider timeProvider)
{
    public async Task EvaluatePlanFlagsForTenantAsync(TenantId tenantId, SubscriptionPlan subscriptionPlan, CancellationToken cancellationToken)
    {
        var subscriptionPlanTier = MapToPlanTier(subscriptionPlan);
        var planFeatureFlagDefinitions = SharedKernel.FeatureFlags.FeatureFlags.GetAll().Where(f => f.RequiredPlan is not null).ToArray();

        if (planFeatureFlagDefinitions.Length == 0) return;

        var existingOverrides = await featureFlagRepository.GetPlanBasedOverridesForTenantAsync(tenantId.Value, cancellationToken);
        var overridesByKey = existingOverrides.ToDictionary(f => f.FlagKey);
        var now = timeProvider.GetUtcNow();
        var changed = false;

        foreach (var definition in planFeatureFlagDefinitions)
        {
            var shouldBeEnabled = subscriptionPlanTier >= definition.RequiredPlan!.Value;
            overridesByKey.TryGetValue(definition.Key, out var existingOverride);

            if (shouldBeEnabled)
            {
                if (existingOverride is null)
                {
                    var featureFlag = FeatureFlag.CreateTenantOverride(definition.Key, tenantId.Value, FeatureFlagSource.Plan);
                    featureFlag.Activate(now);
                    await featureFlagRepository.AddAsync(featureFlag, cancellationToken);
                    changed = true;
                }
                else if (!existingOverride.IsActive)
                {
                    existingOverride.Activate(now);
                    featureFlagRepository.Update(existingOverride);
                    changed = true;
                }
            }
            else
            {
                if (existingOverride?.IsActive == true)
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
