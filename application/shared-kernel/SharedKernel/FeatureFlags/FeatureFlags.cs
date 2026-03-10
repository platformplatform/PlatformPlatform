namespace SharedKernel.FeatureFlags;

[PublicAPI]
public static class FeatureFlags
{
    public static readonly FeatureFlagDefinition GoogleOauth = new(
        "google-oauth",
        FeatureFlagScope.System,
        FeatureFlagAdminLevel.SystemAdmin,
        "Google OAuth authentication"
    );

    public static readonly FeatureFlagDefinition Subscriptions = new(
        "subscriptions",
        FeatureFlagScope.System,
        FeatureFlagAdminLevel.SystemAdmin,
        "Subscription billing via Stripe"
    );

    public static readonly FeatureFlagDefinition BetaFeatures = new(
        "beta-features",
        FeatureFlagScope.Tenant,
        FeatureFlagAdminLevel.SystemAdmin,
        "Enables beta features for tenants",
        IsAbTestEligible: true,
        TrackInTelemetry: true
    );

    public static readonly FeatureFlagDefinition Sso = new(
        "sso",
        FeatureFlagScope.Tenant,
        FeatureFlagAdminLevel.SystemAdmin,
        "Enables single sign-on for tenants"
    );

    public static readonly FeatureFlagDefinition CustomBranding = new(
        "custom-branding",
        FeatureFlagScope.Tenant,
        FeatureFlagAdminLevel.TenantOwner,
        "Enables custom branding options for tenants",
        ConfigurableByTenant: true
    );

    public static readonly FeatureFlagDefinition CompactView = new(
        "compact-view",
        FeatureFlagScope.User,
        FeatureFlagAdminLevel.User,
        "Enables compact view in the user interface",
        ConfigurableByUser: true
    );

    private static readonly FeatureFlagDefinition[] AllFlags = [GoogleOauth, Subscriptions, BetaFeatures, Sso, CustomBranding, CompactView];

    static FeatureFlags()
    {
        ValidateFlags();
    }

    public static FeatureFlagDefinition[] GetAll()
    {
        return AllFlags;
    }

    public static FeatureFlagDefinition? Get(string key)
    {
        return AllFlags.FirstOrDefault(f => f.Key == key);
    }

    private static void ValidateFlags()
    {
        var flagsByKey = AllFlags.ToDictionary(f => f.Key);

        foreach (var flag in AllFlags)
        {
            if (flag.Key.Length > 50)
            {
                throw new InvalidOperationException($"Feature flag key '{flag.Key}' exceeds 50 characters.");
            }

            if (flag.Key.Contains(','))
            {
                throw new InvalidOperationException($"Feature flag key '{flag.Key}' must not contain commas.");
            }

            switch (flag.Scope)
            {
                case FeatureFlagScope.System when flag.AdminLevel != FeatureFlagAdminLevel.SystemAdmin:
                    throw new InvalidOperationException($"Feature flag '{flag.Key}' with System scope must use SystemAdmin admin level.");
                case FeatureFlagScope.Tenant when flag.AdminLevel is not (FeatureFlagAdminLevel.SystemAdmin or FeatureFlagAdminLevel.TenantOwner):
                    throw new InvalidOperationException($"Feature flag '{flag.Key}' with Tenant scope must use SystemAdmin or TenantOwner admin level.");
                case FeatureFlagScope.User when flag.AdminLevel != FeatureFlagAdminLevel.User:
                    throw new InvalidOperationException($"Feature flag '{flag.Key}' with User scope must use User admin level.");
            }

            if (flag.ConfigurableByTenant && (flag.Scope != FeatureFlagScope.Tenant || flag.AdminLevel != FeatureFlagAdminLevel.TenantOwner))
            {
                throw new InvalidOperationException($"Feature flag '{flag.Key}' can only be ConfigurableByTenant when Scope=Tenant and AdminLevel=TenantOwner.");
            }

            if (flag.ConfigurableByUser && (flag.Scope != FeatureFlagScope.User || flag.AdminLevel != FeatureFlagAdminLevel.User))
            {
                throw new InvalidOperationException($"Feature flag '{flag.Key}' can only be ConfigurableByUser when Scope=User and AdminLevel=User.");
            }

            if (flag is { ConfigurableByTenant: true, IsAbTestEligible: true })
            {
                throw new InvalidOperationException($"Feature flag '{flag.Key}' cannot be both ConfigurableByTenant and IsAbTestEligible.");
            }

            if (flag.ParentDependency is not null)
            {
                if (!flagsByKey.TryGetValue(flag.ParentDependency, out var parent))
                {
                    throw new InvalidOperationException($"Feature flag '{flag.Key}' references non-existent parent dependency '{flag.ParentDependency}'.");
                }

                if (parent.ParentDependency is not null)
                {
                    throw new InvalidOperationException($"Feature flag '{flag.Key}' has parent '{flag.ParentDependency}' which itself has a parent dependency. Only one level of dependency is allowed.");
                }
            }
        }
    }
}
