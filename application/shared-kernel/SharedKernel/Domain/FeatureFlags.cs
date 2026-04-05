using System.Text.Json;

namespace SharedKernel.Domain;

[PublicAPI]
public static class FeatureFlags
{
    private static readonly FeatureFlagDefinition[] AllFeatureFlags;
    private static readonly Dictionary<FeatureFlagKey, FeatureFlagDefinition> FeatureFlagsByKey;

    static FeatureFlags()
    {
        AllFeatureFlags = LoadFromEmbeddedResource();
        FeatureFlagsByKey = AllFeatureFlags.ToDictionary(f => f.Key);
        ValidateFlags();
    }

    public static FeatureFlagDefinition[] GetAll()
    {
        return AllFeatureFlags;
    }

    public static FeatureFlagDefinition? Get(string key)
    {
        return FeatureFlagsByKey.GetValueOrDefault(new FeatureFlagKey(key));
    }

    public static FeatureFlagDefinition? Get(FeatureFlagKey key)
    {
        return FeatureFlagsByKey.GetValueOrDefault(key);
    }

    private static FeatureFlagDefinition[] LoadFromEmbeddedResource()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "SharedKernel.Domain.feature-flags.json";

        using var stream = assembly.GetManifestResourceStream(resourceName)
                           ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
        var entries = JsonSerializer.Deserialize<FeatureFlagJsonEntry[]>(stream, options)
                      ?? throw new InvalidOperationException("Failed to deserialize feature-flags.json.");

        return entries.Select(ParseEntry).ToArray();
    }

    private static FeatureFlagDefinition ParseEntry(FeatureFlagJsonEntry entry)
    {
        return entry.Type switch
        {
            FeatureFlagType.System => new SystemFeatureFlagDefinition(
                entry.FeatureFlagKey,
                entry.Description,
                entry.SystemConfigKey ?? throw new InvalidOperationException($"Feature flag '{entry.FeatureFlagKey}' of type 'System' must have 'systemConfigKey'.")
            ),
            FeatureFlagType.SubscriptionPlan => new SubscriptionPlanFeatureFlagDefinition(
                entry.FeatureFlagKey,
                entry.Description,
                entry.RequiredSubscriptionPlan ?? throw new InvalidOperationException($"Feature flag '{entry.FeatureFlagKey}' of type 'SubscriptionPlan' must have 'requiredSubscriptionPlan'.")
            ),
            FeatureFlagType.Tenant => new TenantFeatureFlagDefinition(
                entry.FeatureFlagKey,
                entry.Description,
                entry.AdminLevel ?? FeatureFlagAdminLevel.SystemAdmin,
                entry.IsAbTestEligible ?? false,
                entry.ConfigurableByTenant ?? false
            ),
            FeatureFlagType.User => new UserFeatureFlagDefinition(
                entry.FeatureFlagKey,
                entry.Description,
                entry.IsAbTestEligible ?? false,
                entry.ConfigurableByUser ?? false
            ),
            _ => throw new InvalidOperationException($"Feature flag '{entry.FeatureFlagKey}' has unknown type '{entry.Type}'.")
        };
    }

    private static void ValidateFlags()
    {
        var keys = new HashSet<FeatureFlagKey>();

        foreach (var featureFlag in AllFeatureFlags)
        {
            if (!keys.Add(featureFlag.Key))
            {
                throw new InvalidOperationException($"Duplicate feature flag key '{featureFlag.Key}'.");
            }

            if (featureFlag.Key.Value.Length > 50)
            {
                throw new InvalidOperationException($"Feature flag key '{featureFlag.Key}' exceeds 50 characters.");
            }

            if (featureFlag.Key.Value.Contains(','))
            {
                throw new InvalidOperationException($"Feature flag key '{featureFlag.Key}' must not contain commas.");
            }

            if (featureFlag is SystemFeatureFlagDefinition { IsAbTestEligible: true })
            {
                throw new InvalidOperationException($"System feature flag '{featureFlag.Key}' cannot be A/B test eligible.");
            }

            if (featureFlag is SystemFeatureFlagDefinition { ConfigurableByTenant: true } or SystemFeatureFlagDefinition { ConfigurableByUser: true })
            {
                throw new InvalidOperationException($"System feature flag '{featureFlag.Key}' cannot be configurable.");
            }

            if (featureFlag is SubscriptionPlanFeatureFlagDefinition { IsAbTestEligible: true })
            {
                throw new InvalidOperationException($"Subscription plan feature flag '{featureFlag.Key}' cannot be A/B test eligible.");
            }

            if (featureFlag is SubscriptionPlanFeatureFlagDefinition { ConfigurableByTenant: true } or SubscriptionPlanFeatureFlagDefinition { ConfigurableByUser: true })
            {
                throw new InvalidOperationException($"Subscription plan feature flag '{featureFlag.Key}' cannot be configurable.");
            }

            if (featureFlag is TenantFeatureFlagDefinition { ConfigurableByUser: true })
            {
                throw new InvalidOperationException($"Tenant feature flag '{featureFlag.Key}' cannot have configurableByUser.");
            }

            if (featureFlag is UserFeatureFlagDefinition { ConfigurableByTenant: true })
            {
                throw new InvalidOperationException($"User feature flag '{featureFlag.Key}' cannot have configurableByTenant.");
            }

            if (featureFlag is TenantFeatureFlagDefinition { ConfigurableByTenant: true, IsAbTestEligible: true })
            {
                throw new InvalidOperationException($"Feature flag '{featureFlag.Key}' cannot be both ConfigurableByTenant and IsAbTestEligible.");
            }

            if (featureFlag is TenantFeatureFlagDefinition { ConfigurableByTenant: true, AdminLevel: not FeatureFlagAdminLevel.TenantOwner })
            {
                throw new InvalidOperationException($"Feature flag '{featureFlag.Key}' can only be ConfigurableByTenant when AdminLevel=TenantOwner.");
            }
        }
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    private sealed record FeatureFlagJsonEntry
    {
        public required FeatureFlagKey FeatureFlagKey { get; init; }

        public required FeatureFlagType Type { get; init; }

        public required string Description { get; init; }

        public string? SystemConfigKey { get; init; }

        public SubscriptionPlan? RequiredSubscriptionPlan { get; init; }

        public FeatureFlagAdminLevel? AdminLevel { get; init; }

        public bool? IsAbTestEligible { get; init; }

        public bool? ConfigurableByTenant { get; init; }

        public bool? ConfigurableByUser { get; init; }
    }
}
