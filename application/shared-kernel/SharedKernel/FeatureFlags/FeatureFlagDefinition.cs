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
    public string Key { get; } = key;

    public string Label { get; } = label;

    public string Description { get; } = description;

    public abstract FeatureFlagScope Scope { get; }

    public abstract FeatureFlagAdminLevel AdminLevel { get; }

    public virtual string? ParentDependency => null;

    public virtual bool IsAbTestEligible => false;

    public virtual bool ConfigurableByTenant => false;

    public virtual bool ConfigurableByUser => false;

    public virtual bool TrackInTelemetry => false;

    public virtual string? TelemetryName => null;

    public virtual PlanTier? RequiredPlan => null;

    public virtual string? SystemConfigKey => null;

    public virtual string? SystemConfigExpectedValue => null;

    public virtual string? FrontendEnvVar => null;

    public virtual bool IsKillSwitchEnabled => false;

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
    string? systemConfigExpectedValue = null
) : FeatureFlagDefinition(key, label, description)
{
    public override FeatureFlagScope Scope => FeatureFlagScope.System;

    public override FeatureFlagAdminLevel AdminLevel => FeatureFlagAdminLevel.SystemAdmin;

    public override string SystemConfigKey => systemConfigKey;

    public override string FrontendEnvVar => frontendEnvVar;

    public override string? SystemConfigExpectedValue => systemConfigExpectedValue;
}

// Tenant scope, A/B-eligible, system-admin-managed. Used for flags rolled out gradually across the
// tenant population (e.g., beta features). Cannot be ConfigurableByTenant because the rollout is
// admin-driven.
[PublicAPI]
public sealed class TenantAbTestFlag(
    string key,
    string label,
    string description,
    string? parentDependency = null,
    bool trackInTelemetry = false,
    string? telemetryName = null,
    bool isKillSwitchEnabled = false
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
    PlanTier requiredPlan
) : FeatureFlagDefinition(key, label, description)
{
    public override FeatureFlagScope Scope => FeatureFlagScope.Tenant;

    public override FeatureFlagAdminLevel AdminLevel => FeatureFlagAdminLevel.SystemAdmin;

    public override PlanTier? RequiredPlan => requiredPlan;
}

// Tenant scope, owner-toggled. Tenant owners flip these on/off in account settings. Mutually
// exclusive with A/B-rollout by construction.
[PublicAPI]
public sealed class TenantOwnerConfigurableFlag(
    string key,
    string label,
    string description,
    bool isKillSwitchEnabled = false
) : FeatureFlagDefinition(key, label, description)
{
    public override FeatureFlagScope Scope => FeatureFlagScope.Tenant;

    public override FeatureFlagAdminLevel AdminLevel => FeatureFlagAdminLevel.TenantOwner;

    public override bool ConfigurableByTenant => true;

    public override bool IsKillSwitchEnabled => isKillSwitchEnabled;
}

// User scope, user-toggled. Individual users flip these on/off in their preferences.
[PublicAPI]
public sealed class UserConfigurableFlag(
    string key,
    string label,
    string description,
    bool isKillSwitchEnabled = false
) : FeatureFlagDefinition(key, label, description)
{
    public override FeatureFlagScope Scope => FeatureFlagScope.User;

    public override FeatureFlagAdminLevel AdminLevel => FeatureFlagAdminLevel.User;

    public override bool ConfigurableByUser => true;

    public override bool IsKillSwitchEnabled => isKillSwitchEnabled;
}

// User scope, A/B-eligible. Used for per-user experimental UI rollouts.
[PublicAPI]
public sealed class UserAbTestFlag(
    string key,
    string label,
    string description,
    bool trackInTelemetry = false,
    string? telemetryName = null,
    bool isKillSwitchEnabled = false
) : FeatureFlagDefinition(key, label, description)
{
    public override FeatureFlagScope Scope => FeatureFlagScope.User;

    public override FeatureFlagAdminLevel AdminLevel => FeatureFlagAdminLevel.User;

    public override bool IsAbTestEligible => true;

    public override bool TrackInTelemetry => trackInTelemetry;

    public override string? TelemetryName => telemetryName;

    public override bool IsKillSwitchEnabled => isKillSwitchEnabled;
}
