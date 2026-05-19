using System.Text.Json;

namespace SharedKernel.Platform;

public sealed class Settings
{
    private static readonly Lazy<Settings> Instance = new(LoadFromEmbeddedResource);

    public static Settings Current => Instance.Value;

    public required IdentityConfig Identity { get; init; }

    public required BrandingConfig Branding { get; init; }

    public required SocialLinksConfig SocialLinks { get; init; }

    private static Settings LoadFromEmbeddedResource()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "platform-settings.jsonc";

        using var stream = assembly.GetManifestResourceStream(resourceName)
                           ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip
        };

        var settings = JsonSerializer.Deserialize<Settings>(stream, options)
                       ?? throw new InvalidOperationException("Failed to deserialize platform settings");

        ValidateTaglineLocaleParity(settings.Branding.Tagline);
        return settings;
    }

    private static void ValidateTaglineLocaleParity(TaglineConfig tagline)
    {
        var webLocales = tagline.Web.Keys.OrderBy(locale => locale, StringComparer.Ordinal).ToArray();
        var mailLocales = tagline.Mail.Keys.OrderBy(locale => locale, StringComparer.Ordinal).ToArray();

        if (webLocales.Length == 0)
        {
            throw new InvalidOperationException("platform-settings.jsonc: branding.tagline.web must contain at least one locale.");
        }

        if (!webLocales.SequenceEqual(mailLocales, StringComparer.Ordinal))
        {
            throw new InvalidOperationException(
                $"platform-settings.jsonc: branding.tagline.web and branding.tagline.mail must define the same locales. Web has [{string.Join(", ", webLocales)}], mail has [{string.Join(", ", mailLocales)}]."
            );
        }
    }

    public sealed class IdentityConfig
    {
        public required string InternalEmailDomain { get; init; }
    }

    public sealed class BrandingConfig
    {
        public required string ProductName { get; init; }

        public required ThemeColorConfig ThemeColor { get; init; }

        public required string BackgroundColor { get; init; }

        public required string EmailHeaderBackground { get; init; }

        public required PrimaryColorConfig PrimaryColor { get; init; }

        public required TaglineConfig Tagline { get; init; }

        public required string ContactEmail { get; init; }

        public required string SupportEmail { get; init; }

        public required bool ShowAddToHomescreen { get; init; }
    }

    public sealed class ThemeColorConfig
    {
        public required string Light { get; init; }

        public required string Dark { get; init; }
    }

    public sealed class PrimaryColorConfig
    {
        public required string Light { get; init; }

        public required string LightForeground { get; init; }

        public required string Dark { get; init; }

        public required string DarkForeground { get; init; }
    }

    public sealed class TaglineConfig
    {
        public required IReadOnlyDictionary<string, string> Web { get; init; }

        public required IReadOnlyDictionary<string, string> Mail { get; init; }
    }

    public sealed class SocialLinksConfig
    {
        public required string GitHub { get; init; }

        public required string LinkedIn { get; init; }

        public required string YouTube { get; init; }

        public required string X { get; init; }

        public required string Facebook { get; init; }

        public required string Instagram { get; init; }
    }
}
