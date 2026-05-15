namespace SharedKernel.FeatureFlags;

// Add a new feature flag by declaring a `public static readonly FeatureFlagDefinition` field below.
// The registry in FeatureFlagsRegistry.cs picks it up by reflection at startup — no manual list
// maintenance, no DI wiring, no JSON. Choose the subtype (SystemFeatureFlag, TenantAbTestFlag,
// PlanGatedTenantFlag, TenantOwnerConfigurableFlag, UserAbTestFlag, UserConfigurableFlag) that
// matches how the flag should be evaluated and who is allowed to change it.
//
// On startup, the Account Worker's FeatureFlagDefinitionReconciler upserts a row in the
// feature_flags table for every non-System flag declared here, so it shows up in the Back Office
// immediately after deployment — no migration, no seed script. SystemFeatureFlag definitions are
// evaluated from config and env vars instead, so they never get a DB row. Removing a flag marks
// its row as orphaned but keeps it visible until you hard-delete it from the Back Office.
//
// The label and description below are also surfaced in the frontend: `build --backend` runs the
// GenerateFeatureFlagsManifest MSBuild target which emits featureFlags.generated.json, and the
// shared-webapp generateFeatureFlagArtifacts.mjs script turns that into labels.generated.ts with
// Lingui `t` macros. Lingui extraction then writes the strings into the locale .po files for
// translators, so every flag added here automatically becomes translatable in the UI.
[PublicAPI]
public static partial class FeatureFlags
{
    public static readonly FeatureFlagDefinition GoogleOauth = new SystemFeatureFlag(
        "google-oauth",
        "Google OAuth",
        "Sign in with Google using OpenID Connect",
        "OAuth:Google:ClientId",
        "PUBLIC_GOOGLE_OAUTH_ENABLED",
        false
    );

    public static readonly FeatureFlagDefinition Subscriptions = new SystemFeatureFlag(
        "subscriptions",
        "Subscriptions",
        "Stripe-powered subscription billing and plan management",
        "Stripe:SubscriptionEnabled",
        "PUBLIC_SUBSCRIPTION_ENABLED",
        false,
        "true"
    );

    public static readonly FeatureFlagDefinition BetaFeatures = new TenantAbTestFlag(
        "beta-features",
        "Beta features",
        "Early access to experimental features before general availability",
        true,
        true
    );

    public static readonly FeatureFlagDefinition Sso = new PlanGatedTenantFlag(
        "sso",
        "Single sign-on",
        "Allow users to authenticate using enterprise identity providers",
        PlanTier.Premium,
        false
    );

    public static readonly FeatureFlagDefinition AccountOverview = new TenantOwnerConfigurableFlag(
        "account-overview",
        "Account overview page",
        "Show the account overview dashboard with user statistics at /account. When disabled, signed-in users go straight to the users list.",
        true,
        true
    );

    public static readonly FeatureFlagDefinition CompactView = new UserConfigurableFlag(
        "compact-view",
        "Compact view",
        "Reduce spacing between UI elements for a denser layout",
        true,
        true
    );

    public static readonly FeatureFlagDefinition ExperimentalUi = new UserAbTestFlag(
        "experimental-ui",
        "Experimental UI",
        "Try out experimental user interface components",
        true,
        true
    );
}
