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
    string? TelemetryName = null
);
