using Microsoft.Extensions.Configuration;

namespace SharedKernel.Domain;

[PublicAPI]
public abstract record FeatureFlagDefinition(
    string Key,
    FeatureFlagScope Scope,
    FeatureFlagAdminLevel AdminLevel,
    string Description,
    string? ParentDependency = null
)
{
    public virtual bool IsAbTestEligible => false;

    public virtual bool ConfigurableByTenant => false;

    public virtual bool ConfigurableByUser => false;

    public virtual bool TrackInTelemetry => true;

    public virtual SubscriptionPlan? RequiredPlan => null;
}

[PublicAPI]
public sealed record SystemFeatureFlagDefinition(
    string Key,
    string Description,
    string SystemConfigKey,
    string? SystemConfigExpectedValue = null,
    string? ParentDependency = null
) : FeatureFlagDefinition(Key, FeatureFlagScope.System, FeatureFlagAdminLevel.SystemAdmin, Description, ParentDependency)
{
    public override bool TrackInTelemetry => false;

    public bool IsSystemFeatureFlagEnabled(IConfiguration configuration)
    {
        var configValue = configuration[SystemConfigKey];

        return SystemConfigExpectedValue is not null
            ? configValue == SystemConfigExpectedValue
            : !string.IsNullOrEmpty(configValue);
    }
}

[PublicAPI]
public sealed record SubscriptionPlanFeatureFlagDefinition(
    string Key,
    string Description,
    SubscriptionPlan Plan,
    string? ParentDependency = null
) : FeatureFlagDefinition(Key, FeatureFlagScope.Tenant, FeatureFlagAdminLevel.SystemAdmin, Description, ParentDependency)
{
    public override SubscriptionPlan? RequiredPlan => Plan;
}

[PublicAPI]
public sealed record TenantFeatureFlagDefinition(
    string Key,
    string Description,
    FeatureFlagAdminLevel AdminLevel = FeatureFlagAdminLevel.SystemAdmin,
    bool IsAbTestEligible = false,
    bool ConfigurableByTenant = false,
    string? ParentDependency = null
) : FeatureFlagDefinition(Key, FeatureFlagScope.Tenant, AdminLevel, Description, ParentDependency)
{
    public override bool IsAbTestEligible { get; } = IsAbTestEligible;

    public override bool ConfigurableByTenant { get; } = ConfigurableByTenant;
}

[PublicAPI]
public sealed record UserFeatureFlagDefinition(
    string Key,
    string Description,
    bool IsAbTestEligible = false,
    bool ConfigurableByUser = false,
    string? ParentDependency = null
) : FeatureFlagDefinition(Key, FeatureFlagScope.User, FeatureFlagAdminLevel.User, Description, ParentDependency)
{
    public override bool IsAbTestEligible { get; } = IsAbTestEligible;

    public override bool ConfigurableByUser { get; } = ConfigurableByUser;
}
