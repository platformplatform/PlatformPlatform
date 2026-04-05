using Microsoft.Extensions.Configuration;

namespace SharedKernel.Domain;

[PublicAPI]
public abstract record FeatureFlagDefinition(
    FeatureFlagKey Key,
    FeatureFlagScope Scope,
    FeatureFlagAdminLevel AdminLevel,
    string Description
)
{
    public virtual bool IsAbTestEligible => false;

    public virtual bool ConfigurableByTenant => false;

    public virtual bool ConfigurableByUser => false;

    public virtual SubscriptionPlan? RequiredSubscriptionPlan => null;
}

[PublicAPI]
public sealed record SystemFeatureFlagDefinition(
    FeatureFlagKey Key,
    string Description,
    string SystemConfigKey
) : FeatureFlagDefinition(Key, FeatureFlagScope.System, FeatureFlagAdminLevel.SystemAdmin, Description)
{
    public bool IsSystemFeatureFlagEnabled(IConfiguration configuration)
    {
        return configuration[SystemConfigKey] == "true";
    }
}

[PublicAPI]
public sealed record SubscriptionPlanFeatureFlagDefinition(
    FeatureFlagKey Key,
    string Description,
    SubscriptionPlan Plan
) : FeatureFlagDefinition(Key, FeatureFlagScope.Tenant, FeatureFlagAdminLevel.SystemAdmin, Description)
{
    public override SubscriptionPlan? RequiredSubscriptionPlan => Plan;
}

[PublicAPI]
public sealed record TenantFeatureFlagDefinition(
    FeatureFlagKey Key,
    string Description,
    FeatureFlagAdminLevel AdminLevel,
    bool IsAbTestEligible,
    bool ConfigurableByTenant
) : FeatureFlagDefinition(Key, FeatureFlagScope.Tenant, AdminLevel, Description)
{
    public override bool IsAbTestEligible { get; } = IsAbTestEligible;

    public override bool ConfigurableByTenant { get; } = ConfigurableByTenant;
}

[PublicAPI]
public sealed record UserFeatureFlagDefinition(
    FeatureFlagKey Key,
    string Description,
    bool IsAbTestEligible,
    bool ConfigurableByUser
) : FeatureFlagDefinition(Key, FeatureFlagScope.User, FeatureFlagAdminLevel.User, Description)
{
    public override bool IsAbTestEligible { get; } = IsAbTestEligible;

    public override bool ConfigurableByUser { get; } = ConfigurableByUser;
}
