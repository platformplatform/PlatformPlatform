namespace SharedKernel.FeatureFlags;

public static class FeatureFlagTelemetryProperties
{
    // Single `user.feature_flags` tag with a comma-separated, sorted list of enabled flag keys. Mirrors
    // the `user.locale` / `user.role` / `user.theme` shape elsewhere on the same telemetry item and
    // avoids the OpenTelemetry-reserved `feature_flag.*` semantic-convention namespace, which is a
    // fixed closed set (`feature_flag.key`, `feature_flag.result.value`, ...) that vendor integrations
    // route to feature-flag-specific dashboards. Iteration is over current C# definitions only —
    // orphaned flag keys never reach telemetry because the reconciler marks them OrphanedAt at startup
    // and they drop out of `GetAll()`. Returns null when no trackable flag is enabled so an empty tag
    // is not emitted on every telemetry item.
    public const string TagName = "user.feature_flags";

    public static (string Name, string Value)? GetEnabledFeatureFlagsTag(IReadOnlySet<string> enabledFlags)
    {
        var trackable = FeatureFlags.GetAll()
            .Where(f => f.TrackInTelemetry && enabledFlags.Contains(f.Key))
            .Select(f => f.TelemetryName ?? f.Key)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        return trackable.Length == 0 ? null : (TagName, string.Join(",", trackable));
    }
}
