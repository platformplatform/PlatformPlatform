using Microsoft.Extensions.Configuration;

namespace SharedKernel.FeatureFlags;

[PublicAPI]
public sealed record FeatureFlagDefinition(
    string Key,
    FeatureFlagScope Scope,
    FeatureFlagAdminLevel AdminLevel,
    string Description,
    string? ParentDependency = null,
    bool IsAbTestEligible = false,
    bool ConfigurableByTenant = false,
    bool ConfigurableByUser = false,
    bool TrackInTelemetry = false,
    string? TelemetryName = null,
    PlanTier? RequiredPlan = null,
    string? SystemConfigKey = null,
    string? SystemConfigExpectedValue = null,
    bool IsKillSwitchEnabled = false
)
{
    public bool IsSystemFeatureFlagEnabled(IConfiguration configuration)
    {
        if (Scope != FeatureFlagScope.System || SystemConfigKey is null) return false;

        var configValue = configuration[SystemConfigKey];

        return SystemConfigExpectedValue is not null
            ? configValue == SystemConfigExpectedValue
            : !string.IsNullOrEmpty(configValue);
    }
}
