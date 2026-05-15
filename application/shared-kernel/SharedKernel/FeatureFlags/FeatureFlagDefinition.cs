using Microsoft.Extensions.Configuration;

namespace SharedKernel.FeatureFlags;

// Abstract base. Each valid combination of Scope, AdminLevel, and capability flags lives in its own
// sealed subtype, so illegal combinations are compile-time errors rather than runtime throws in
// FeatureFlags.ValidateFlags(). The flat virtual property API is preserved so the ~20 consumer files
// (evaluator, queries, command handlers, telemetry, manifest emitter, reconciler) keep reading
// `flag.Scope`, `flag.IsAbTestEligible`, etc. unchanged. Subtypes override only the properties
// relevant to their flavor and leave the rest at the safe defaults below.
[PublicAPI]
public abstract class FeatureFlagDefinition(string key, string label, string description)
{
    /// <summary>
    ///     Stable identifier used in URLs, telemetry property names, JWT claims, and the feature_flags DB column.
    ///     Lowercase kebab-case, max 50 chars.
    /// </summary>
    public string Key { get; } = key;

    /// <summary>Human-readable name shown in the BackOffice and (via Lingui codegen) in user-facing settings.</summary>
    public string Label { get; } = label;

    /// <summary>Human-readable description shown in tooltips and detail pages.</summary>
    public string Description { get; } = description;

    /// <summary>Where the flag is evaluated. System = env/config; Tenant = per-tenant DB row; User = per-user DB row.</summary>
    public abstract FeatureFlagScope Scope { get; }

    /// <summary>
    ///     Who is allowed to change the flag's state. Mapped to back-office and account-app policies at the controller
    ///     level.
    /// </summary>
    public abstract FeatureFlagAdminLevel AdminLevel { get; }

    /// <summary>Another flag's Key that must be enabled for this flag to evaluate true. Only one level of nesting allowed.</summary>
    public virtual string? ParentDependency => null;

    /// <summary>
    ///     Gradual rollout via BucketStart/BucketEnd. Mutually exclusive with ConfigurableByTenant/User — admins drive
    ///     the rollout, not end users.
    /// </summary>
    public virtual bool IsAbTestEligible => false;

    /// <summary>Tenant owners can toggle the flag from account settings. Cannot combine with IsAbTestEligible.</summary>
    public virtual bool ConfigurableByTenant => false;

    /// <summary>End users can toggle the flag from preferences. Cannot combine with IsAbTestEligible.</summary>
    public virtual bool ConfigurableByUser => false;

    /// <summary>Emit the flag's evaluated value as a property on every telemetry event so analytics can segment on it.</summary>
    public abstract bool TrackInTelemetry { get; }

    /// <summary>Override for the telemetry property name. Null = use Key verbatim.</summary>
    public virtual string? TelemetryName => null;

    /// <summary>
    ///     Subscription plan tier that activates this flag. Reconciler binds Source=Plan when set so the plan evaluator
    ///     owns the row.
    /// </summary>
    public virtual PlanTier? RequiredPlan => null;

    /// <summary>appsettings.json key consulted by IsSystemFeatureFlagEnabled. System scope only.</summary>
    public virtual string? SystemConfigKey => null;

    /// <summary>
    ///     Exact value the SystemConfigKey must equal to enable the flag. Null = any non-empty value is treated as
    ///     enabled.
    /// </summary>
    public virtual string? SystemConfigExpectedValue => null;

    /// <summary>
    ///     Runtime env-var name the frontend reads via import.meta.runtime_env to evaluate the flag client-side. System
    ///     scope only.
    /// </summary>
    public virtual string? FrontendEnvVar => null;

    /// <summary>
    ///     True = the reconciler creates the base row inactive on first sight so an admin must explicitly enable it;
    ///     false = activated automatically.
    /// </summary>
    public virtual bool IsKillSwitchEnabled => false;

    /// <summary>
    ///     True = the flag represents a stable module that is always on; the BackOffice hides the
    ///     Activate/Deactivate toggle so admins can't accidentally kill it. False = a regular feature
    ///     flag that admins can globally deactivate.
    /// </summary>
    public virtual bool IsStableModule => false;

    public bool IsSystemFeatureFlagEnabled(IConfiguration configuration)
    {
        if (Scope != FeatureFlagScope.System || SystemConfigKey is null) return false;

        var configValue = configuration[SystemConfigKey];

        return SystemConfigExpectedValue is not null
            ? configValue == SystemConfigExpectedValue
            : !string.IsNullOrEmpty(configValue);
    }
}

// System scope: env-var-driven kill switch. SystemConfigKey and FrontendEnvVar are required by the
// constructor so a SystemFeatureFlag can never be built without both.
[PublicAPI]
public sealed class SystemFeatureFlag(
    string key,
    string label,
    string description,
    string systemConfigKey,
    string frontendEnvVar,
    bool trackInTelemetry,
    string? systemConfigExpectedValue = null
) : FeatureFlagDefinition(key, label, description)
{
    public override FeatureFlagScope Scope => FeatureFlagScope.System;

    public override FeatureFlagAdminLevel AdminLevel => FeatureFlagAdminLevel.SystemAdmin;

    public override string SystemConfigKey => systemConfigKey;

    public override string FrontendEnvVar => frontendEnvVar;

    public override string? SystemConfigExpectedValue => systemConfigExpectedValue;

    public override bool TrackInTelemetry => trackInTelemetry;
}

// Tenant scope, A/B-eligible, system-admin-managed. Used for flags rolled out gradually across the
// tenant population (e.g., beta features). Cannot be ConfigurableByTenant because the rollout is
// admin-driven.
[PublicAPI]
public sealed class TenantAbTestFlag(
    string key,
    string label,
    string description,
    bool trackInTelemetry,
    bool isKillSwitchEnabled,
    string? parentDependency = null,
    string? telemetryName = null
) : FeatureFlagDefinition(key, label, description)
{
    public override FeatureFlagScope Scope => FeatureFlagScope.Tenant;

    public override FeatureFlagAdminLevel AdminLevel => FeatureFlagAdminLevel.SystemAdmin;

    public override bool IsAbTestEligible => true;

    public override string? ParentDependency => parentDependency;

    public override bool TrackInTelemetry => trackInTelemetry;

    public override string? TelemetryName => telemetryName;

    public override bool IsKillSwitchEnabled => isKillSwitchEnabled;
}

// Tenant scope, plan-gated. Activation is driven by the tenant's subscription plan — no manual
// admin toggling, no A/B rollout, no kill switch (the plan is the source of truth).
[PublicAPI]
public sealed class PlanGatedTenantFlag(
    string key,
    string label,
    string description,
    PlanTier requiredPlan,
    bool trackInTelemetry
) : FeatureFlagDefinition(key, label, description)
{
    public override FeatureFlagScope Scope => FeatureFlagScope.Tenant;

    public override FeatureFlagAdminLevel AdminLevel => FeatureFlagAdminLevel.SystemAdmin;

    public override PlanTier? RequiredPlan => requiredPlan;

    public override bool TrackInTelemetry => trackInTelemetry;
}

// Tenant scope, owner-toggled. Tenant owners flip these on/off in account settings. Mutually
// exclusive with A/B-rollout by construction.
[PublicAPI]
public sealed class TenantOwnerConfigurableFlag(
    string key,
    string label,
    string description,
    bool trackInTelemetry,
    bool isKillSwitchEnabled,
    bool isStableModule = false
) : FeatureFlagDefinition(key, label, description)
{
    public override FeatureFlagScope Scope => FeatureFlagScope.Tenant;

    public override FeatureFlagAdminLevel AdminLevel => FeatureFlagAdminLevel.TenantOwner;

    public override bool ConfigurableByTenant => true;

    public override bool TrackInTelemetry => trackInTelemetry;

    public override bool IsKillSwitchEnabled => isKillSwitchEnabled;

    public override bool IsStableModule => isStableModule;
}

// User scope, user-toggled. Individual users flip these on/off in their preferences.
[PublicAPI]
public sealed class UserConfigurableFlag(
    string key,
    string label,
    string description,
    bool trackInTelemetry,
    bool isKillSwitchEnabled,
    bool isStableModule = false
) : FeatureFlagDefinition(key, label, description)
{
    public override FeatureFlagScope Scope => FeatureFlagScope.User;

    public override FeatureFlagAdminLevel AdminLevel => FeatureFlagAdminLevel.User;

    public override bool ConfigurableByUser => true;

    public override bool TrackInTelemetry => trackInTelemetry;

    public override bool IsKillSwitchEnabled => isKillSwitchEnabled;

    public override bool IsStableModule => isStableModule;
}

// User scope, A/B-eligible. Used for per-user experimental UI rollouts.
[PublicAPI]
public sealed class UserAbTestFlag(
    string key,
    string label,
    string description,
    bool trackInTelemetry,
    bool isKillSwitchEnabled,
    string? telemetryName = null
) : FeatureFlagDefinition(key, label, description)
{
    public override FeatureFlagScope Scope => FeatureFlagScope.User;

    public override FeatureFlagAdminLevel AdminLevel => FeatureFlagAdminLevel.User;

    public override bool IsAbTestEligible => true;

    public override bool TrackInTelemetry => trackInTelemetry;

    public override string? TelemetryName => telemetryName;

    public override bool IsKillSwitchEnabled => isKillSwitchEnabled;
}
