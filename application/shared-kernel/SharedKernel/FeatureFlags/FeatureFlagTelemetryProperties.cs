namespace SharedKernel.FeatureFlags;

public static class FeatureFlagTelemetryProperties
{
    // Per-flag tags namespaced under `user.feature_flags.*` for User-scope flags and
    // `tenant.feature_flags.*` for Tenant-scope flags. Plural `feature_flags` keeps us clear of
    // the OpenTelemetry-reserved `feature_flag.*` semantic-convention namespace, which is a fixed
    // closed set (`feature_flag.key`, `feature_flag.result.value`, ...) routed by vendor
    // integrations to feature-flag-specific dashboards. Splitting by scope lets KQL group by who
    // carries each setting — `tenant.feature_flags.beta-features` is a property of the tenant,
    // `user.feature_flags.compact-view` is a property of the user. The value is `enabled`; the
    // tag is absent when the flag is not enabled, so KQL can filter on presence
    // (`isnotempty(customDimensions["user.feature_flags.compact-view"])`). Iteration is over
    // current C# definitions only — orphaned flag keys never reach telemetry because the
    // reconciler marks them OrphanedAt at startup and they drop out of `GetAll()`.
    public const string UserScopePrefix = "user.feature_flags.";
    public const string TenantScopePrefix = "tenant.feature_flags.";
    public const string EnabledValue = "enabled";

    public static IEnumerable<(string Name, string Value)> GetEnabledFeatureFlagTags(IReadOnlySet<string> enabledFlags)
    {
        foreach (var definition in FeatureFlags.GetAll())
        {
            if (!definition.TrackInTelemetry) continue;
            if (!enabledFlags.Contains(definition.Key)) continue;

            var prefix = definition.Scope switch
            {
                FeatureFlagScope.Tenant => TenantScopePrefix,
                FeatureFlagScope.User => UserScopePrefix,
                _ => null
            };

            if (prefix is null) continue;

            var telemetryKey = definition.TelemetryName ?? definition.Key;
            yield return ($"{prefix}{telemetryKey}", EnabledValue);
        }
    }
}
