using System.Security;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace PlatformPlatform.SharedKernel.ApiCore.SinglePageApp;

public class SinglePageAppConfiguration
{
    public const string PublicUrlKey = "PUBLIC_URL";
    public const string CdnUrlKey = "CDN_URL";
    private const string PublicKeyPrefix = "PUBLIC_";
    private const string ApplicationVersionKey = "APPLICATION_VERSION";

    public static readonly string BuildRootPath = GetWebAppDistRoot("WebApp", "dist");
    private static readonly DateTime StartupTime = DateTime.UtcNow;

    private readonly string _htmlTemplatePath;
    private readonly bool _isDevelopment;
    private readonly string[] _publicAllowedKeys = [CdnUrlKey, ApplicationVersionKey];
    private string? _htmlTemplate;

    public SinglePageAppConfiguration(IOptions<JsonOptions> jsonOptions, bool isDevelopment)
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

        var json = JsonSerializer.Serialize(StaticRuntimeEnvironment, jsonOptions.Value.SerializerOptions);
        StaticRuntimeEnvironmentEncoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));

        VerifyRuntimeEnvironment(StaticRuntimeEnvironment);

        _isDevelopment = isDevelopment;
        _htmlTemplatePath = Path.Combine(BuildRootPath, "index.html");
        PermissionPolicies = GetPermissionsPolicies();
        ContentSecurityPolicies = GetContentSecurityPolicies();
    }

    private string CdnUrl { get; }

    private string PublicUrl { get; }

    public Dictionary<string, string> StaticRuntimeEnvironment { get; }

    public string StaticRuntimeEnvironmentEncoded { get; }

    public StringValues PermissionPolicies { get; }

    public string ContentSecurityPolicies { get; }

    public string GetHtmlTemplate()
    {
        if (_htmlTemplate is not null && !_isDevelopment)
        {
            return _htmlTemplate;
        }

        AwaitSinglePageAppGeneration();

        if (!File.Exists(_htmlTemplatePath))
        {
            throw new FileNotFoundException("index.html does not exist.", _htmlTemplatePath);
        }

        _htmlTemplate = File.ReadAllText(_htmlTemplatePath, new UTF8Encoding());
        return _htmlTemplate;
    }

    /// <summary>
    ///     When debugging locally, the SPA and API are built in parallel, so we give the SPA a few seconds to be ready.
    ///     It takes a few seconds from when the index.html is written to disk, until the JS and CSS are ready.
    /// </summary>
    [Conditional("DEBUG")]
    private void AwaitSinglePageAppGeneration()
    {
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed < TimeSpan.FromSeconds(30))
        {
            if (File.Exists(_htmlTemplatePath) && new FileInfo(_htmlTemplatePath).CreationTimeUtc > StartupTime.AddSeconds(-10)) break;
            Thread.Sleep(TimeSpan.FromMilliseconds(2));
        }

        // From the time the index.html is created, the Web App Dev server needs a few moments to warm up
        Thread.Sleep(TimeSpan.FromMilliseconds(500));
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

    private StringValues GetPermissionsPolicies()
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
        var trustedCdnHosts = "https://platformplatformgithub.blob.core.windows.net";
        var trustedHosts = $"{PublicUrl} {CdnUrl} {trustedCdnHosts}";

        if (_isDevelopment)
        {
            var webSocketHost = CdnUrl.Replace("https", "wss");
            trustedHosts += $" {webSocketHost}";
        }

        var contentSecurityPolicies = new[]
        {
            $"script-src {trustedHosts} 'strict-dynamic' https:",
            $"script-src-elem {trustedHosts}",
            $"default-src {trustedHosts}",
            $"connect-src {trustedHosts}",
            $"img-src {trustedHosts} data:",
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
