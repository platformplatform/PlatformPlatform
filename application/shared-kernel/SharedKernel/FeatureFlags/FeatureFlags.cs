using System.Text.RegularExpressions;

namespace SharedKernel.FeatureFlags;

[PublicAPI]
public static class FeatureFlags
{
    public static readonly FeatureFlagDefinition GoogleOauth = new SystemFeatureFlag(
        "google-oauth",
        "Google OAuth",
        "Sign in with Google using OpenID Connect",
        "OAuth:Google:ClientId",
        "PUBLIC_GOOGLE_OAUTH_ENABLED"
    );

    public static readonly FeatureFlagDefinition Subscriptions = new SystemFeatureFlag(
        "subscriptions",
        "Subscriptions",
        "Stripe-powered subscription billing and plan management",
        "Stripe:SubscriptionEnabled",
        "PUBLIC_SUBSCRIPTION_ENABLED",
        "true"
    );

    public static readonly FeatureFlagDefinition BetaFeatures = new TenantAbTestFlag(
        "beta-features",
        "Beta features",
        "Early access to experimental features before general availability",
        trackInTelemetry: true,
        isKillSwitchEnabled: true
    );

    public static readonly FeatureFlagDefinition Sso = new PlanGatedTenantFlag(
        "sso",
        "Single sign-on",
        "Allow users to authenticate using enterprise identity providers",
        PlanTier.Premium
    );

    public static readonly FeatureFlagDefinition AccountOverview = new TenantOwnerConfigurableFlag(
        "account-overview",
        "Account overview page",
        "Show the account overview dashboard with user statistics at /account. When disabled, signed-in users go straight to the users list.",
        isKillSwitchEnabled: true
    );

    public static readonly FeatureFlagDefinition CompactView = new UserConfigurableFlag(
        "compact-view",
        "Compact view",
        "Reduce spacing between UI elements for a denser layout",
        isKillSwitchEnabled: true
    );

    public static readonly FeatureFlagDefinition ExperimentalUi = new UserAbTestFlag(
        "experimental-ui",
        "Experimental UI",
        "Try out experimental user interface components",
        true,
        isKillSwitchEnabled: true
    );

    // Reflected at startup over every `public static readonly FeatureFlagDefinition` field on this
    // class — adding a new flag means just declaring the field; no manual array maintenance.
    private static readonly FeatureFlagDefinition[] AllFeatureFlags =
        typeof(FeatureFlags)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsInitOnly && f.FieldType == typeof(FeatureFlagDefinition))
            .Select(f => (FeatureFlagDefinition)f.GetValue(null)!)
            .ToArray();

    private static readonly Regex FeatureFlagKeyPattern =
        new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

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

    // Keys are surfaced verbatim in URLs, JWT claim payloads, telemetry property names, frontend
    // route params, and the configurable-feature-flag toggle UI. Lowercase kebab-case keeps every
    // consumer's case-handling simple and removes ambiguity for the comma-separated `feature_flags`
    // JWT claim. Internal so SharedKernel.Tests can pin the pattern.
    internal static bool IsValidKey(string key)
    {
        return FeatureFlagKeyPattern.IsMatch(key);
    }

    // Subtype hierarchy in FeatureFlagDefinition.cs enforces all cross-property invariants at compile
    // time. This method now validates only what subtypes cannot: key format, parent-dependency
    // existence + depth (parent must exist in the registry and itself have no parent), and
    // TelemetryName uniqueness (since telemetry property names must remain stable for forever).
    private static void ValidateFlags()
    {
        var featureFlagsByKey = AllFeatureFlags.ToDictionary(f => f.Key);
        var telemetryNamesSeen = new Dictionary<string, string>();

        foreach (var featureFlag in AllFeatureFlags)
        {
            if (featureFlag.Key.Length > 50)
            {
                throw new InvalidOperationException($"Feature flag key '{featureFlag.Key}' exceeds 50 characters.");
            }

            if (!IsValidKey(featureFlag.Key))
            {
                throw new InvalidOperationException($"Feature flag key '{featureFlag.Key}' must be lowercase kebab-case (a-z, 0-9, hyphen). No leading/trailing hyphens, no consecutive hyphens, no other characters.");
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

            if (featureFlag.TelemetryName is not null)
            {
                if (telemetryNamesSeen.TryGetValue(featureFlag.TelemetryName, out var existingKey))
                {
                    throw new InvalidOperationException($"Feature flag '{featureFlag.Key}' uses TelemetryName '{featureFlag.TelemetryName}' which is already used by '{existingKey}'. TelemetryName must be unique across all flags so historical telemetry stays unambiguous.");
                }

                telemetryNamesSeen[featureFlag.TelemetryName] = featureFlag.Key;
            }
        }
    }
}
