using System.Text.Json;

namespace PlatformPlatform.SharedKernel.Platform;

public sealed class Settings
{
    private static readonly Lazy<Settings> Instance = new(LoadFromEmbeddedResource);

    public static Settings Current => Instance.Value;

    public required IdentityConfig Identity { get; init; }

    public required BrandingConfig Branding { get; init; }

    private static Settings LoadFromEmbeddedResource()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "PlatformPlatform.SharedKernel.Platform.platform-settings.jsonc";

        using var stream = assembly.GetManifestResourceStream(resourceName)
                           ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        return JsonSerializer.Deserialize<Settings>(stream, options)
               ?? throw new InvalidOperationException("Failed to deserialize platform settings");
    }

    public sealed class IdentityConfig
    {
        public required string InternalEmailDomain { get; init; }
    }

    public sealed class BrandingConfig
    {
        public required string ProductName { get; init; }

        public required string SupportEmail { get; init; }
    }
}
