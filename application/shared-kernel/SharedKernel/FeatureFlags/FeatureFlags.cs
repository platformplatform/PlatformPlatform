namespace SharedKernel.FeatureFlags;

[PublicAPI]
public static class FeatureFlags
{
    public static readonly FeatureFlagDefinition GoogleOauth = new(
        "google-oauth",
        FeatureFlagScope.System,
        FeatureFlagAdminLevel.SystemAdmin,
        "Google OAuth",
        "Sign in with Google using OpenID Connect",
        SystemConfigKey: "OAuth:Google:ClientId",
        FrontendEnvVar: "PUBLIC_GOOGLE_OAUTH_ENABLED"
    );

    public static readonly FeatureFlagDefinition Subscriptions = new(
        "subscriptions",
        FeatureFlagScope.System,
        FeatureFlagAdminLevel.SystemAdmin,
        "Subscriptions",
        "Stripe-powered subscription billing and plan management",
        SystemConfigKey: "Stripe:SubscriptionEnabled",
        SystemConfigExpectedValue: "true",
        FrontendEnvVar: "PUBLIC_SUBSCRIPTION_ENABLED"
    );

    public static readonly FeatureFlagDefinition BetaFeatures = new(
        "beta-features",
        FeatureFlagScope.Tenant,
        FeatureFlagAdminLevel.SystemAdmin,
        "Beta features",
        "Early access to experimental features before general availability",
        IsAbTestEligible: true,
        TrackInTelemetry: true,
        IsKillSwitchEnabled: true
    );

    public static readonly FeatureFlagDefinition Sso = new(
        "sso",
        FeatureFlagScope.Tenant,
        FeatureFlagAdminLevel.SystemAdmin,
        "Single sign-on",
        "Allow users to authenticate using enterprise identity providers",
        RequiredPlan: PlanTier.Premium,
        IsKillSwitchEnabled: false
    );

    public static readonly FeatureFlagDefinition CustomBranding = new(
        "custom-branding",
        FeatureFlagScope.Tenant,
        FeatureFlagAdminLevel.TenantOwner,
        "Custom branding",
        "Customize the login page with your organization's logo and colors",
        ConfigurableByTenant: true,
        IsKillSwitchEnabled: false
    );

    public static readonly FeatureFlagDefinition CompactView = new(
        "compact-view",
        FeatureFlagScope.User,
        FeatureFlagAdminLevel.User,
        "Compact view",
        "Reduce spacing between UI elements for a denser layout",
        ConfigurableByUser: true,
        IsKillSwitchEnabled: false
    );

    public static readonly FeatureFlagDefinition ExperimentalUi = new(
        "experimental-ui",
        FeatureFlagScope.User,
        FeatureFlagAdminLevel.User,
        "Experimental UI",
        "Try out experimental user interface components",
        IsAbTestEligible: true,
        TrackInTelemetry: true,
        IsKillSwitchEnabled: true
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

            if (featureFlag is { Scope: FeatureFlagScope.System, SystemConfigKey: null })
            {
                throw new InvalidOperationException($"Feature flag '{featureFlag.Key}' with System scope must define a SystemConfigKey.");
            }

            if (featureFlag.Scope != FeatureFlagScope.System && featureFlag.SystemConfigKey is not null)
            {
                throw new InvalidOperationException($"Feature flag '{featureFlag.Key}' must not define SystemConfigKey unless Scope is System.");
            }

            switch (featureFlag.Scope)
            {
                case FeatureFlagScope.System when featureFlag.AdminLevel != FeatureFlagAdminLevel.SystemAdmin:
                    throw new InvalidOperationException($"Feature flag '{featureFlag.Key}' with System scope must use SystemAdmin admin level.");
                case FeatureFlagScope.Tenant when featureFlag.AdminLevel is not (FeatureFlagAdminLevel.SystemAdmin or FeatureFlagAdminLevel.TenantOwner):
                    throw new InvalidOperationException($"Feature flag '{featureFlag.Key}' with Tenant scope must use SystemAdmin or TenantOwner admin level.");
                case FeatureFlagScope.User when featureFlag.AdminLevel != FeatureFlagAdminLevel.User:
                    throw new InvalidOperationException($"Feature flag '{featureFlag.Key}' with User scope must use User admin level.");
            }

            if (featureFlag.ConfigurableByTenant && (featureFlag.Scope != FeatureFlagScope.Tenant || featureFlag.AdminLevel != FeatureFlagAdminLevel.TenantOwner))
            {
                throw new InvalidOperationException($"Feature flag '{featureFlag.Key}' can only be ConfigurableByTenant when Scope=Tenant and AdminLevel=TenantOwner.");
            }

            if (featureFlag.ConfigurableByUser && (featureFlag.Scope != FeatureFlagScope.User || featureFlag.AdminLevel != FeatureFlagAdminLevel.User))
            {
                throw new InvalidOperationException($"Feature flag '{featureFlag.Key}' can only be ConfigurableByUser when Scope=User and AdminLevel=User.");
            }

            if (featureFlag is { ConfigurableByTenant: true, IsAbTestEligible: true })
            {
                throw new InvalidOperationException($"Feature flag '{featureFlag.Key}' cannot be both ConfigurableByTenant and IsAbTestEligible.");
            }

            if (featureFlag.RequiredPlan is not null)
            {
                if (featureFlag.Scope != FeatureFlagScope.Tenant)
                {
                    throw new InvalidOperationException($"Feature flag '{featureFlag.Key}' with RequiredPlan must have Tenant scope.");
                }

                if (featureFlag.ConfigurableByTenant)
                {
                    throw new InvalidOperationException($"Feature flag '{featureFlag.Key}' with RequiredPlan cannot be ConfigurableByTenant.");
                }

                if (featureFlag.ConfigurableByUser)
                {
                    throw new InvalidOperationException($"Feature flag '{featureFlag.Key}' with RequiredPlan cannot be ConfigurableByUser.");
                }

                if (featureFlag.IsAbTestEligible)
                {
                    throw new InvalidOperationException($"Feature flag '{featureFlag.Key}' with RequiredPlan cannot be IsAbTestEligible.");
                }

                if (featureFlag.IsKillSwitchEnabled)
                {
                    throw new InvalidOperationException($"Feature flag '{featureFlag.Key}' with RequiredPlan cannot be IsKillSwitchEnabled - plan-gated flags must always be platform-managed.");
                }
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
