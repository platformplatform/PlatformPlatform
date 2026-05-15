using System.Text.Json;
using JetBrains.Annotations;
using SharedKernel.FeatureFlags;

namespace Account.Api;

// Build-time tool that serializes the feature flag registry to JSON. Invoked by the
// GenerateFeatureFlagsManifest MSBuild target in Account.Api.csproj after Build via
// `dotnet "$(TargetPath)" --emit-feature-flags-manifest <path>`. The frontend codegen
// script application/shared-webapp/ui/scripts/generateFeatureFlagArtifacts.mjs reads
// the emitted JSON and produces labels.generated.ts + registry.generated.ts.
internal static class FeatureFlagsManifestEmitter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    public static void Emit(string manifestPath)
    {
        var directory = Path.GetDirectoryName(manifestPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var manifest = FeatureFlags.GetAll().Select(featureFlag => new FeatureFlagManifestEntry(
                featureFlag.Key,
                featureFlag.Scope.ToString(),
                featureFlag.AdminLevel.ToString(),
                featureFlag.Label,
                featureFlag.Description,
                featureFlag.ParentDependency,
                featureFlag.IsAbTestEligible,
                featureFlag.ConfigurableByTenant,
                featureFlag.ConfigurableByUser,
                featureFlag.TrackInTelemetry,
                featureFlag.TelemetryName,
                featureFlag.RequiredPlan?.ToString(),
                featureFlag.SystemConfigKey,
                featureFlag.SystemConfigExpectedValue,
                featureFlag.FrontendEnvVar,
                featureFlag.IsKillSwitchEnabled,
                featureFlag.IsStableModule
            )
        ).ToArray();

        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, Options) + Environment.NewLine);
    }

    [UsedImplicitly]
    private sealed record FeatureFlagManifestEntry(
        string Key,
        string Scope,
        string AdminLevel,
        string Label,
        string Description,
        string? ParentDependency,
        bool IsAbTestEligible,
        bool ConfigurableByTenant,
        bool ConfigurableByUser,
        bool TrackInTelemetry,
        string? TelemetryName,
        string? RequiredPlan,
        string? SystemConfigKey,
        string? SystemConfigExpectedValue,
        string? FrontendEnvVar,
        bool IsKillSwitchEnabled,
        bool IsStableModule
    );
}
