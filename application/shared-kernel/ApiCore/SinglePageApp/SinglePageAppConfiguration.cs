using System.Security;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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

    private readonly string _htmlTemplatePath;
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

        _htmlTemplatePath = Path.Combine(BuildRootPath, "index.html");
        PermissionPolicies = GetPermissionsPolicies();
        ContentSecurityPolicies = GetContentSecurityPolicies(isDevelopment);
    }

    private string CdnUrl { get; }

    private string PublicUrl { get; }

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
    ///     When debugging locally, the frontend is served from a Web Development server over web sockets.
    ///     The build server generates the artifacts incrementally, so here we check if the artifacts are fully generated.
    ///     Also, this awaits the Application Insights file to be fully generated, as it is created in two steps.
    /// </summary>
    [Conditional("DEBUG")]
    private void AwaitSinglePageAppGeneration()
    {
        var startNew = Stopwatch.StartNew();
        while (startNew.Elapsed < TimeSpan.FromSeconds(30))
        {
            if (File.Exists(_htmlTemplatePath))
            {
                var htmlTemplate = File.ReadAllText(_htmlTemplatePath, new UTF8Encoding());
                if (string.IsNullOrEmpty(htmlTemplate))
                {
                    continue; // The index.html is first written empty and then filled with content
                }

                var applicationInsightsUrl = ExtractApplicationInsightsUrl(htmlTemplate);

                if (GetApplicationInsightsBundleSize(applicationInsightsUrl).Result > 2_000_000)
                {
                    return; // The Application Insights file is generated in two steps, the final size is above 2 MB
                }

                Console.WriteLine("Waiting for Application Insights file to be fully generated.");
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(200));
        }

        throw new FileNotFoundException("index.html does not exist.", _htmlTemplatePath);

        string ExtractApplicationInsightsUrl(string htmlContent)
        {
            // The Application Insights script URL has a hash in the filename, so it's extracted dynamically
            var regex = new Regex(
                @"src=""%CDN_URL%(/static/js/[\w-/]*applicationinsights-react-js[\w-]*\.js)""",
                RegexOptions.IgnoreCase,
                TimeSpan.FromSeconds(1)
            );
            var match = regex.Match(htmlContent);
            if (match.Success)
            {
                var path = match.Groups[1].Value;
                var host = Environment.GetEnvironmentVariable("CDN_URL");
                return $"{host}{path}";
            }

            throw new InvalidOperationException("Application Insights script URL not found.");
        }

        async Task<int> GetApplicationInsightsBundleSize(string applicationInsightsUrl)
        {
            try
            {
                var httpClient = new HttpClient { Timeout = TimeSpan.FromMilliseconds(500) };
                var response = await httpClient.GetAsync(applicationInsightsUrl);
                if (!response.IsSuccessStatusCode) return 0;
                var content = await response.Content.ReadAsByteArrayAsync();
                return content.Length;
            }
            catch (HttpRequestException)
            {
                return 0;
            }
            catch (TaskCanceledException)
            {
                return 0;
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
