using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Account.Tests.FeatureFlags;

// Codegen contract: every public instance property defined on every concrete FeatureFlagDefinition
// subtype must appear in the entry of the generated manifest for at least one flag
// (application/shared-webapp/ui/featureFlags/featureFlags.generated.json). The manifest is the
// source-of-truth bridge between C# and the TS registry consumed by useFeatureFlag. Reflecting over
// the base class would miss subtype-only properties (a new `MyNewCapability { get; }` on a future
// subtype), which is exactly the drift class the contract test exists to prevent.
public sealed class FeatureFlagsManifestContractTests
{
    [Fact]
    public void EveryPublicPropertyOnEveryFlagSubtype_AppearsInManifestEntry()
    {
        var manifestPath = ResolveManifestPath();
        File.Exists(manifestPath).Should().BeTrue($"manifest must exist at '{manifestPath}'; run `build --backend` to regenerate");

        var manifestJson = File.ReadAllText(manifestPath);
        using var manifest = JsonDocument.Parse(manifestJson);
        manifest.RootElement.ValueKind.Should().Be(JsonValueKind.Array);
        manifest.RootElement.GetArrayLength().Should().BeGreaterThan(0);

        var manifestKeys = manifest.RootElement.EnumerateArray()
            .SelectMany(entry => entry.EnumerateObject().Select(p => p.Name))
            .ToHashSet(StringComparer.Ordinal);

        var expectedKeys = global::SharedKernel.FeatureFlags.FeatureFlags.GetAll()
            .SelectMany(f => f.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            .Select(p => ToCamelCase(p.Name))
            .ToHashSet(StringComparer.Ordinal);

        var missing = expectedKeys.Except(manifestKeys).ToArray();
        missing.Should().BeEmpty(
            $"At least one FeatureFlagDefinition subtype exposes properties that the manifest emitter does not project. "
            + $"Add these to FeatureFlagsManifestEmitter.cs and regenerate: {string.Join(", ", missing)}"
        );
    }

    private static string ToCamelCase(string value)
    {
        if (string.IsNullOrEmpty(value) || char.IsLower(value[0])) return value;
        return char.ToLowerInvariant(value[0]) + value[1..];
    }

    private static string ResolveManifestPath()
    {
        // Walk up from the test binary directory to the repository root, then descend into the
        // generated artifacts location. Matches the layout that the MSBuild target writes to.
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !Directory.Exists(Path.Combine(directory.FullName, "application")))
        {
            directory = directory.Parent;
        }

        if (directory is null) throw new InvalidOperationException("Could not locate repository root from test binary directory.");

        return Path.Combine(directory.FullName, "application", "shared-webapp", "ui", "featureFlags", "featureFlags.generated.json");
    }
}
