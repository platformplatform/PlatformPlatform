namespace SharedKernel.Domain;

[PublicAPI]
public static class FeatureFlags
{
    public static readonly SystemFeatureFlagDefinition GoogleOauth = new(
        "google-oauth",
        "Google OAuth authentication",
        "OAuth:Google:ClientId"
    );

    public static readonly SystemFeatureFlagDefinition Subscriptions = new(
        "subscriptions",
        "Subscription billing via Stripe",
        "Stripe:SubscriptionEnabled",
        "true"
    );

    public static readonly TenantFeatureFlagDefinition BetaFeatures = new(
        "beta-features",
        "Enables beta features for tenants",
        IsAbTestEligible: true
    );

    public static readonly SubscriptionPlanFeatureFlagDefinition Sso = new(
        "sso",
        "Enables single sign-on for tenants",
        SubscriptionPlan.Premium
    );

    public static readonly TenantFeatureFlagDefinition CustomBranding = new(
        "custom-branding",
        "Enables custom branding options for tenants",
        FeatureFlagAdminLevel.TenantOwner,
        ConfigurableByTenant: true
    );

    public static readonly UserFeatureFlagDefinition CompactView = new(
        "compact-view",
        "Enables compact view in the user interface",
        ConfigurableByUser: true
    );

    public static readonly UserFeatureFlagDefinition ExperimentalUi = new(
        "experimental-ui",
        "Enables experimental UI components for users",
        true
    );

    private static readonly FeatureFlagDefinition[] AllFeatureFlags = [GoogleOauth, Subscriptions, BetaFeatures, Sso, CustomBranding, CompactView, ExperimentalUi];

    static FeatureFlags()
    {
        ValidateFlags();
    }

    public static FeatureFlagDefinition[] GetAll()
    {
        return AllFeatureFlags;
    }

    public static FeatureFlagDefinition? Get(string key)
    {
        return AllFeatureFlags.FirstOrDefault(f => f.Key == key);
    }

    private static void ValidateFlags()
    {
        var featureFlagsByKey = AllFeatureFlags.ToDictionary(f => f.Key);

        foreach (var featureFlag in AllFeatureFlags)
        {
            if (featureFlag.Key.Length > 50)
            {
                throw new InvalidOperationException($"Feature flag key '{featureFlag.Key}' exceeds 50 characters.");
            }

            if (featureFlag.Key.Contains(','))
            {
                throw new InvalidOperationException($"Feature flag key '{featureFlag.Key}' must not contain commas.");
            }

            if (featureFlag is TenantFeatureFlagDefinition { ConfigurableByTenant: true, IsAbTestEligible: true })
            {
                throw new InvalidOperationException($"Feature flag '{featureFlag.Key}' cannot be both ConfigurableByTenant and IsAbTestEligible.");
            }

            if (featureFlag is TenantFeatureFlagDefinition { ConfigurableByTenant: true, AdminLevel: not FeatureFlagAdminLevel.TenantOwner })
            {
                throw new InvalidOperationException($"Feature flag '{featureFlag.Key}' can only be ConfigurableByTenant when AdminLevel=TenantOwner.");
            }

            if (featureFlag.ParentDependency is not null)
            {
                if (!featureFlagsByKey.TryGetValue(featureFlag.ParentDependency, out var parent))
                {
                    throw new InvalidOperationException($"Feature flag '{featureFlag.Key}' references non-existent parent dependency '{featureFlag.ParentDependency}'.");
                }

                if (parent.ParentDependency is not null)
                {
                    throw new InvalidOperationException($"Feature flag '{featureFlag.Key}' has parent '{featureFlag.ParentDependency}' which itself has a parent dependency. Only one level of dependency is allowed.");
                }
            }
        }
    }
}
