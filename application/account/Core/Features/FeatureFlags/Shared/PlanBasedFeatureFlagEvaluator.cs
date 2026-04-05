using Account.Features.FeatureFlags.Domain;
using Account.Features.Tenants.Domain;
using SharedKernel.Domain;

namespace Account.Features.FeatureFlags.Shared;

public sealed class PlanBasedFeatureFlagEvaluator(IFeatureFlagRepository featureFlagRepository, ITenantRepository tenantRepository, TimeProvider timeProvider)
{
    public async Task EvaluatePlanFlagsForTenantAsync(TenantId tenantId, SubscriptionPlan subscriptionPlan, CancellationToken cancellationToken)
    {
        var planFeatureFlagDefinitions = SharedKernel.Domain.FeatureFlags.GetAll().Where(f => f.RequiredSubscriptionPlan is not null).ToArray();

        if (planFeatureFlagDefinitions.Length == 0) return;

        var existingPlanFeatureFlags = await featureFlagRepository.GetPlanBasedOverridesForTenantAsync(tenantId, cancellationToken);
        var planFeatureFlagsByKey = existingPlanFeatureFlags.ToDictionary(f => f.FeatureFlagKey);
        var now = timeProvider.GetUtcNow();
        var changed = false;

        foreach (var featureFlagDefinition in planFeatureFlagDefinitions)
        {
            var shouldBeEnabled = subscriptionPlan >= featureFlagDefinition.RequiredSubscriptionPlan!.Value;
            planFeatureFlagsByKey.TryGetValue(featureFlagDefinition.Key, out var existingPlanFeatureFlag);

            if (shouldBeEnabled)
            {
                if (existingPlanFeatureFlag is null)
                {
                    var featureFlag = FeatureFlag.CreateTenantOverride(featureFlagDefinition.Key, tenantId, FeatureFlagSource.SubscriptionPlan);
                    featureFlag.Activate(now);
                    await featureFlagRepository.AddAsync(featureFlag, cancellationToken);
                    changed = true;
                }
                else if (!IsActive(existingPlanFeatureFlag))
                {
                    existingPlanFeatureFlag.Activate(now);
                    featureFlagRepository.Update(existingPlanFeatureFlag);
                    changed = true;
                }
            }
            else
            {
                if (existingPlanFeatureFlag is not null && IsActive(existingPlanFeatureFlag))
                {
                    existingPlanFeatureFlag.Deactivate(now);
                    featureFlagRepository.Update(existingPlanFeatureFlag);
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
