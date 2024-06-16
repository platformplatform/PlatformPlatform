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

    private readonly string _htmlTemplatePath;
    private readonly string[] _publicAllowedKeys = [CdnUrlKey, ApplicationVersionKey];
    private readonly DateTime _startUpTime = DateTime.UtcNow;
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

        BuildRootPath = GetWebAppDistRoot("WebApp", "dist");
        _htmlTemplatePath = Path.Combine(GetWebAppDistRoot("WebApp", "dist"), "index.html");
        PermissionPolicies = GetPermissionsPolicies();
        ContentSecurityPolicies = GetContentSecurityPolicies(isDevelopment);
    }

    private string CdnUrl { get; }

    private string PublicUrl { get; }

    public string BuildRootPath { get; }

    public Dictionary<string, string> StaticRuntimeEnvironment { get; }

    public string StaticRuntimeEnvironmentEncoded { get; }

    public StringValues PermissionPolicies { get; }

    public string ContentSecurityPolicies { get; }

    public string GetHtmlTemplate()
    {
        if (_htmlTemplate is not null)
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
    ///     When debugging locally, the SPA and API are built in parallel, so we give the SPA 10 seconds to be ready.
    ///     It takes a few seconds from when the index.html is written to disk, until the JS and CSS are ready.
    /// </summary>
    [Conditional("DEBUG")]
    private void AwaitSinglePageAppGeneration()
    {
        var startNew = Stopwatch.StartNew();
        while (startNew.Elapsed < TimeSpan.FromSeconds(10))
        {
            var spaCreationTimeUtc = new FileInfo(_htmlTemplatePath).CreationTimeUtc;
            if (spaCreationTimeUtc > _startUpTime.AddSeconds(-10) && spaCreationTimeUtc < DateTime.UtcNow.AddSeconds(-2))
            {
                break;
            }
        }
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

    private string GetContentSecurityPolicies(bool isDevelopment)
    {
        var trustedCdnHosts = "https://platformplatformgithub.blob.core.windows.net";
        var trustedHosts = $"{PublicUrl} {CdnUrl} {trustedCdnHosts}";

        if (isDevelopment)
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
