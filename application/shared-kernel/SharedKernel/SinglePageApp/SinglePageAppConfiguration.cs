using System.Security;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.Primitives;
using SharedKernel.Configuration;

namespace SharedKernel.SinglePageApp;

public class SinglePageAppConfiguration
{
    public const string PublicUrlKey = "PUBLIC_URL";
    public const string CdnUrlKey = "CDN_URL";
    public const string DefaultWebAppProjectName = "WebApp";
    private const string PublicKeyPrefix = "PUBLIC_";
    private const string ApplicationVersionKey = "APPLICATION_VERSION";
    public static readonly string[] SupportedLocalizations = ["en-US", "da-DK"];

    // Default bundle directory for callers that host a single SPA (the original layout). Multi-SPA
    // hosts (e.g. consolidated account-api hosting both account/WebApp and account/BackOffice)
    // construct one SinglePageAppConfiguration per SPA via the WebAppProjectName parameter.
    public static readonly string BuildRootPath = GetWebAppDistRoot(DefaultWebAppProjectName, "dist");

    // Source directory for SPA static assets that rsbuild copies into BuildRootPath at build time.
    // Used by UseSinglePageAppFallback to layer public/ assets over the bundle in local dev where
    // rsbuild's dev server holds them.
    public static readonly string PublicRootPath = GetWebAppDistRoot(DefaultWebAppProjectName, "public");

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

    public SinglePageAppConfiguration(
        bool isDevelopment,
        Dictionary<string, string>? environmentVariables,
        string webAppProjectName = DefaultWebAppProjectName,
        string? publicUrlOverride = null,
        string? cdnUrlOverride = null
    )
    {
        // Per-host overrides win over the process-wide env vars so a single process can host multiple SPAs
        // on different hostnames (e.g. consolidated account-api). Env vars remain the source of truth for
        // single-SPA hosts and EF Core migration generation (where neither env vars nor overrides are set).
        PublicUrl = publicUrlOverride ?? Environment.GetEnvironmentVariable(PublicUrlKey) ?? string.Empty;
        CdnUrl = cdnUrlOverride ?? Environment.GetEnvironmentVariable(CdnUrlKey) ?? string.Empty;
        // InformationalVersion preserves the zero-padded format ("2026.05.02.0914") that
        // System.Version-based parsing would otherwise reduce to "2026.5.2.914".
        var applicationVersion =
            Assembly.GetEntryAssembly()!.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? Assembly.GetEntryAssembly()!.GetName().Version!.ToString();

        StaticRuntimeEnvironment = new Dictionary<string, string>
        {
            { PublicUrlKey, PublicUrl },
            { CdnUrlKey, CdnUrl },
            { ApplicationVersionKey, applicationVersion }
        };

        if (environmentVariables is not null)
        {
            foreach (var environmentVariable in environmentVariables)
            {
                StaticRuntimeEnvironment.Add(environmentVariable.Key, environmentVariable.Value);
            }
        }

        var staticRuntimeEnvironmentEncoded = JsonSerializer.Serialize(StaticRuntimeEnvironment, JsonHtmlEncodingOptions);

        // Escape the JSON for use in an HTML attribute
        StaticRuntimeEnvironmentEscaped = HtmlEncoder.Default.Encode(staticRuntimeEnvironmentEncoded);

        VerifyRuntimeEnvironment(StaticRuntimeEnvironment);

        _isDevelopment = isDevelopment;
        BundleDirectory = webAppProjectName == DefaultWebAppProjectName ? BuildRootPath : GetWebAppDistRoot(webAppProjectName, "dist");
        PublicDirectory = GetWebAppDistRoot(webAppProjectName, "public");
        _htmlTemplatePath = Path.Combine(BundleDirectory, "index.html");
        _remoteEntryJsPath = Path.Combine(BundleDirectory, "remoteEntry.js");
        PermissionPolicies = GetPermissionsPolicies();
        ContentSecurityPolicies = GetContentSecurityPolicies();
    }

    public string BundleDirectory { get; }

    // Source directory for SPA static assets that rsbuild copies into BundleDirectory at build time
    // (manifest.json, favicon.ico, apple-touch-icon.png, etc.). In Azure these assets live alongside
    // the bundle, but in local dev rsbuild's dev server holds them and they never reach BundleDirectory.
    // Layering this directory under the bundle file provider closes that gap without per-asset rules.
    public string PublicDirectory { get; }

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
        // Walk up looking for <webAppProjectName>/main.tsx (the React entry point, source-controlled in every
        // SPA) or <webAppProjectName>/<dist>/index.html (production deployments where main.tsx is gone but the
        // built bundle is). Either marker reliably identifies the SPA folder across local dev, CI before the
        // frontend build, and the deployed Azure container. Matching just on the bare folder name would stop
        // at unrelated same-named directories like account/Tests/BackOffice.
        var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        var directoryInfo = new DirectoryInfo(assemblyPath);
        while (directoryInfo is not null)
        {
            var candidate = Path.Join(directoryInfo.FullName, webAppProjectName);
            if (File.Exists(Path.Join(candidate, "main.tsx")) ||
                File.Exists(Path.Join(candidate, webAppDistRootName, "index.html")))
            {
                return Path.Join(candidate, webAppDistRootName);
            }

            directoryInfo = directoryInfo.Parent;
        }

        throw new InvalidOperationException(
            $"Could not locate the SPA project '{webAppProjectName}' walking up from '{assemblyPath}'."
        );
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
            { "fullscreen", ["self"] },
            { "web-share", [] },
            { "identity-credentials-get", [] }
        };

        return string.Join(", ", permissionsPolicies.Select(p => $"{p.Key}=({string.Join(", ", p.Value)})"));
    }

    private string GetContentSecurityPolicies()
    {
        var trustedHosts = $"{PublicUrl} {CdnUrl}";

        if (_isDevelopment)
        {
            var hostname = new Uri(PublicUrl).Host;
            trustedHosts += $" wss://{hostname}:* https://{hostname}:*";
        }

        var contentSecurityPolicies = new[]
        {
            $"script-src {trustedHosts} 'nonce-{{NONCE_PLACEHOLDER}}' 'strict-dynamic' https:",
            $"script-src-elem {trustedHosts} https://js.stripe.com 'nonce-{{NONCE_PLACEHOLDER}}'",
            $"style-src {trustedHosts} 'nonce-{{NONCE_PLACEHOLDER}}'",
            $"style-src-elem {trustedHosts} 'nonce-{{NONCE_PLACEHOLDER}}'",
            $"default-src {trustedHosts}",
            $"connect-src {trustedHosts} https://js.stripe.com https://api.stripe.com data:",
            $"frame-src {trustedHosts} https://js.stripe.com",
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
