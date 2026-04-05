using Account.Features.FeatureFlags.Domain;
using Account.Features.Tenants.Domain;
using SharedKernel.Domain;
using SharedKernel.FeatureFlags;

namespace Account.Features.FeatureFlags.Shared;

public sealed class PlanBasedFeatureFlagEvaluator(IFeatureFlagRepository featureFlagRepository, ITenantRepository tenantRepository, TimeProvider timeProvider)
{
    public async Task EvaluatePlanFlagsForTenantAsync(TenantId tenantId, SubscriptionPlan subscriptionPlan, CancellationToken cancellationToken)
    {
        var planFeatureFlagDefinitions = SharedKernel.FeatureFlags.FeatureFlags.GetAll().Where(f => f.RequiredPlan is not null).ToArray();

        if (planFeatureFlagDefinitions.Length == 0) return;

        var existingOverrides = await featureFlagRepository.GetPlanBasedOverridesForTenantAsync(tenantId.Value, cancellationToken);
        var overridesByKey = existingOverrides.ToDictionary(f => f.FeatureFlagKey);
        var now = timeProvider.GetUtcNow();
        var changed = false;

        foreach (var featureFlagDefinition in planFeatureFlagDefinitions)
        {
            var shouldBeEnabled = subscriptionPlan >= featureFlagDefinition.RequiredPlan!.Value;
            overridesByKey.TryGetValue(featureFlagDefinition.Key, out var existingOverride);

            if (shouldBeEnabled)
            {
                if (existingOverride is null)
                {
                    var featureFlag = FeatureFlag.CreateTenantOverride(featureFlagDefinition.Key, tenantId.Value, FeatureFlagSource.Plan);
                    featureFlag.Activate(now);
                    await featureFlagRepository.AddAsync(featureFlag, cancellationToken);
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

    private static bool IsActive(FeatureFlag featureFlag)
    {
        return featureFlag.EnabledAt is not null && (featureFlag.DisabledAt is null || featureFlag.EnabledAt > featureFlag.DisabledAt);
    }
}
