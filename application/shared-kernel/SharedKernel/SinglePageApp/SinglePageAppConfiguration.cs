using System.Security;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.Primitives;
using PlatformPlatform.SharedKernel.Configuration;

namespace PlatformPlatform.SharedKernel.SinglePageApp;

public class SinglePageAppConfiguration
{
    public const string PublicUrlKey = "PUBLIC_URL";
    public const string CdnUrlKey = "CDN_URL";
    private const string PublicKeyPrefix = "PUBLIC_";
    private const string ApplicationVersionKey = "APPLICATION_VERSION";
    public static readonly string[] SupportedLocalizations = ["en-US", "da-DK", "nl-NL"];

    public static readonly string BuildRootPath = GetWebAppDistRoot("WebApp", "dist");

    public static readonly JsonSerializerOptions JsonHtmlEncodingOptions =
        new(SharedDependencyConfiguration.DefaultJsonSerializerOptions)
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

    private readonly string _htmlTemplatePath;
    private readonly bool _isDevelopment;
    private readonly string[] _publicAllowedKeys = [CdnUrlKey, ApplicationVersionKey];
    private readonly string _remoteEntryJsPath;
    private string? _htmlTemplate;
    private string? _remoteEntryJsContent;

    public SinglePageAppConfiguration(bool isDevelopment, params (string Key, string Value)[] environmentVariables)
    {
        // Environment variables are empty when generating EF Core migrations
        PublicUrl = Environment.GetEnvironmentVariable(PublicUrlKey) ?? string.Empty;
        CdnUrl = Environment.GetEnvironmentVariable(CdnUrlKey) ?? string.Empty;
        var applicationVersion = Assembly.GetEntryAssembly()!.GetName().Version!.ToString();

        StaticRuntimeEnvironment = new Dictionary<string, string>
        {
            { PublicUrlKey, PublicUrl },
            { CdnUrlKey, CdnUrl },
            { ApplicationVersionKey, applicationVersion }
        };

        foreach (var environmentVariable in environmentVariables)
        {
            StaticRuntimeEnvironment.Add(environmentVariable.Key, environmentVariable.Value);
        }

        var staticRuntimeEnvironmentEncoded = JsonSerializer.Serialize(StaticRuntimeEnvironment, JsonHtmlEncodingOptions);

        // Escape the JSON for use in an HTML attribute
        StaticRuntimeEnvironmentEscaped = HtmlEncoder.Default.Encode(staticRuntimeEnvironmentEncoded);

        VerifyRuntimeEnvironment(StaticRuntimeEnvironment);

        _isDevelopment = isDevelopment;
        _htmlTemplatePath = Path.Combine(BuildRootPath, "index.html");
        _remoteEntryJsPath = Path.Combine(BuildRootPath, "remoteEntry.js");
        PermissionPolicies = GetPermissionsPolicies();
        ContentSecurityPolicies = GetContentSecurityPolicies();
    }

    private string CdnUrl { get; }

    private string PublicUrl { get; }

    public Dictionary<string, string> StaticRuntimeEnvironment { get; }

    public string StaticRuntimeEnvironmentEscaped { get; }

    public StringValues PermissionPolicies { get; }

    public string ContentSecurityPolicies { get; }

    public string GetHtmlTemplate()
    {
        AwaitSinglePageAppGeneration();
        return _htmlTemplate ??= File.ReadAllText(_htmlTemplatePath, new UTF8Encoding());
    }

    /// <summary>
    ///     This only runs locally, where the frontend is generating the index.html at startup, and it might not exist.
    ///     This method is called every time, so any changes to the index.html will be updated while debugging.
    ///     In rare cases, the index.html contains RsBuild info when generating it. A simple reload will fix this.
    /// </summary>
    [Conditional("DEBUG")]
    private void AwaitSinglePageAppGeneration()
    {
        var tryUntil = DateTime.Now.AddSeconds(30);
        while (DateTime.Now < tryUntil)
        {
            if (File.Exists(_htmlTemplatePath))
            {
                _htmlTemplate = File.ReadAllText(_htmlTemplatePath, new UTF8Encoding());
                return;
            }

            Thread.Sleep(TimeSpan.FromSeconds(1));
        }
    }

    public string GetRemoteEntryJs()
    {
        return _remoteEntryJsContent ??= File.ReadAllText(_remoteEntryJsPath, new UTF8Encoding());
    }

    private static string GetWebAppDistRoot(string webAppProjectName, string webAppDistRootName)
    {
        var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        var directoryInfo = new DirectoryInfo(assemblyPath);
        while (directoryInfo!.GetDirectories(webAppProjectName).Length == 0 &&
               !Path.Exists(Path.Join(directoryInfo.FullName, webAppProjectName, webAppDistRootName))
              )
        {
            directoryInfo = directoryInfo.Parent;
        }

        return Path.Join(directoryInfo.FullName, webAppProjectName, webAppDistRootName);
    }

    private static StringValues GetPermissionsPolicies()
    {
        var permissionsPolicies = new Dictionary<string, string[]>
        {
            { "geolocation", [] },
            { "microphone", [] },
            { "camera", [] },
            { "picture-in-picture", [] },
            { "display-capture", [] },
            { "fullscreen", [] },
            { "web-share", [] },
            { "identity-credentials-get", [] }
        };

        return string.Join(", ", permissionsPolicies.Select(p => $"{p.Key}=({string.Join(", ", p.Value)})"));
    }

    private string GetContentSecurityPolicies()
    {
        const string trustedCdnHost = "https://platformplatformgithub.blob.core.windows.net";
        var trustedHosts = $"{PublicUrl} {CdnUrl} {trustedCdnHost}";

        if (_isDevelopment)
        {
            var hostname = new Uri(PublicUrl).Host;
            trustedHosts += $" wss://{hostname}:* https://{hostname}:*";
        }

        var contentSecurityPolicies = new[]
        {
            $"script-src {trustedHosts} 'strict-dynamic' https:",
            $"script-src-elem {trustedHosts}",
            $"default-src {trustedHosts}",
            $"connect-src {trustedHosts} data:",
            $"img-src {trustedHosts} data: blob:",
            "object-src 'none'",
            "base-uri 'none'"
            // "require-trusted-types-for 'script'"
        };

        return string.Join(";", contentSecurityPolicies);
    }

    private void VerifyRuntimeEnvironment(Dictionary<string, string> environmentVariables)
    {
        foreach (var key in environmentVariables.Keys)
        {
            if (key.StartsWith(PublicKeyPrefix) || _publicAllowedKeys.Contains(key)) continue;

            throw new SecurityException($"Environment variable '{key}' is not allowed to be public.");
        }
    }
}
