namespace SharedKernel.FeatureFlags;

public static class FeatureFlagTelemetryProperties
{
    // Iteration is over current C# definitions only; orphaned flag keys (DB rows whose key was removed
    // from FeatureFlags.cs) cannot reach telemetry because FeatureFlagDefinitionReconciler marks them
    // OrphanedAt at startup and they are no longer in GetAll(). If a future change loads flags from the
    // database instead of definitions, the orphan filter must be re-introduced here explicitly.
    public static IEnumerable<(string Name, string Value)> Enumerate(IReadOnlySet<string> enabledFlags)
    {
        foreach (var featureFlag in FeatureFlags.GetAll())
        {
            if (!featureFlag.TrackInTelemetry) continue;
            var telemetryName = featureFlag.TelemetryName ?? featureFlag.Key;
            var value = enabledFlags.Contains(featureFlag.Key) ? "enabled" : "disabled";
            yield return ($"feature_flag.{telemetryName}", value);
        }
    }
}
