using System.Security;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using PlatformPlatform.SharedKernel.InfrastructureCore;

namespace PlatformPlatform.SharedKernel.ApiCore.Middleware;

public sealed class WebAppMiddleware
{
    private const string PublicKeyPrefix = "PUBLIC_";
    public const string PublicUrlKey = "PUBLIC_URL";
    public const string CdnUrlKey = "CDN_URL";
    private const string Locale = "LOCALE";
    public const string ApplicationVersion = "APPLICATION_VERSION";

    private readonly string _cdnUrl;
    private readonly string _htmlTemplatePath;
    private readonly bool _isDevelopment;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly RequestDelegate _next;
    private readonly string[] _publicAllowedKeys = [CdnUrlKey, ApplicationVersion];
    private readonly string _publicUrl;
    private readonly Dictionary<string, string> _staticRuntimeEnvironment;
    private string? _htmlTemplate;

    public WebAppMiddleware(
        RequestDelegate next,
        Dictionary<string, string> staticRuntimeEnvironment,
        string htmlTemplatePath,
        IOptions<JsonOptions> jsonOptions
    )
    {
        _next = next;
        _staticRuntimeEnvironment = staticRuntimeEnvironment;
        _htmlTemplatePath = htmlTemplatePath;
        _jsonSerializerOptions = jsonOptions.Value.SerializerOptions;

        VerifyRuntimeEnvironment(staticRuntimeEnvironment);

        _isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "development";
        _cdnUrl = staticRuntimeEnvironment.GetValueOrDefault(CdnUrlKey)!;
        _publicUrl = staticRuntimeEnvironment.GetValueOrDefault(PublicUrlKey)!;
    }

    private void VerifyRuntimeEnvironment(Dictionary<string, string> environmentVariables)
    {
        foreach (var key in environmentVariables.Keys)
        {
            if (key.StartsWith(PublicKeyPrefix) || _publicAllowedKeys.Contains(key)) continue;

            throw new SecurityException($"Environment variable '{key}' is not allowed to be public.");
        }
    }

    private StringValues GetContentSecurityPolicy()
    {
        var devServerWebsocket = _cdnUrl.Replace("https", "wss");

        string[] trustedHosts = _isDevelopment
            ? [_publicUrl, _cdnUrl, devServerWebsocket]
            : [_publicUrl, _cdnUrl];

        var contentSecurityPolicies = new Dictionary<string, string[]>
        {
            { "script-src", trustedHosts.Concat(["'strict-dynamic'", "https:"]).ToArray() },
            { "script-src-elem", trustedHosts },
            { "default-src", trustedHosts },
            { "connect-src", trustedHosts },
            { "img-src", trustedHosts.Append("data:").ToArray() },
            { "object-src", ["'none'"] },
            { "base-uri", ["'none'"] }
            // { "require-trusted-types-for", ["'script'"] }
        };

        return string.Join(
            " ",
            contentSecurityPolicies.Select(policy => $"{policy.Key} {string.Join(" ", policy.Value)};")
        );
    }

    public Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.ToString().StartsWith("/api/")) return _next(context);

        var cultureFeature = context.Features.Get<IRequestCultureFeature>();
        var userCulture = cultureFeature?.RequestCulture.Culture;

        var requestEnvironmentVariables = new Dictionary<string, string> { { Locale, userCulture?.Name ?? "en-US" } };

        // Cache control
        ApplyNoCacheHeaders(context);
        // Content security policy
        context.Response.Headers.Append("Content-Security-Policy", GetContentSecurityPolicy());
        // Set content type
        context.Response.Headers.Append("Content-Type", "text/html; charset=utf-8");

        return context.Response.WriteAsync(GetHtmlWithEnvironment(requestEnvironmentVariables));
    }

    private string GetHtmlWithEnvironment(Dictionary<string, string>? requestEnvironmentVariables = null)
    {
        if (_htmlTemplate is null || _isDevelopment)
        {
            _htmlTemplate = File.ReadAllText(_htmlTemplatePath, new UTF8Encoding());
        }

        var runtimeEnvironment = requestEnvironmentVariables is null
            ? _staticRuntimeEnvironment
            : _staticRuntimeEnvironment.Concat(requestEnvironmentVariables).ToDictionary();

        var encodedRuntimeEnvironment = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(runtimeEnvironment, _jsonSerializerOptions))
        );
        var result = _htmlTemplate.Replace("%ENCODED_RUNTIME_ENV%", encodedRuntimeEnvironment);

        foreach (var variable in runtimeEnvironment)
        {
            result = result.Replace($"%{variable.Key}%", variable.Value);
        }

        return result;
    }

    private static void ApplyNoCacheHeaders(HttpContext context)
    {
        context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
        context.Response.Headers.Append("Pragma", "no-cache");
    }
}

public static class WebAppMiddlewareExtensions
{
    [UsedImplicitly]
    public static IApplicationBuilder UseWebAppMiddleware(
        this IApplicationBuilder builder,
        string webAppProjectName = "WebApp",
        Dictionary<string, string>? publicEnvironmentVariables = null
    )
    {
        if (InfrastructureCoreConfiguration.SwaggerGenerator) return builder;

        var publicUrl = GetEnvironmentVariableOrThrow(WebAppMiddleware.PublicUrlKey);
        var cdnUrl = GetEnvironmentVariableOrThrow(WebAppMiddleware.CdnUrlKey);
        var applicationVersion = Assembly.GetEntryAssembly()!.GetName().Version!.ToString();

        var environmentVariables = new Dictionary<string, string>
        {
            { WebAppMiddleware.PublicUrlKey, publicUrl },
            { WebAppMiddleware.CdnUrlKey, cdnUrl },
            { WebAppMiddleware.ApplicationVersion, applicationVersion }
        };

        var staticRuntimeEnvironment = publicEnvironmentVariables is null
            ? environmentVariables
            : environmentVariables.Concat(publicEnvironmentVariables).ToDictionary();

        var buildRootPath = GetWebAppDistRoot(webAppProjectName, "dist");
        var templateFilePath = Path.Combine(buildRootPath, "index.html");

        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "development")
        {
            for (var i = 0; i < 10; i++)
            {
                if (File.Exists(templateFilePath)) break;
                Debug.WriteLine($"Waiting for {webAppProjectName} build to be ready...");
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }

        if (!File.Exists(templateFilePath))
        {
            throw new FileNotFoundException("index.html does not exist.", templateFilePath);
        }

        return builder
            .UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(buildRootPath) })
            .UseRequestLocalization("en-US", "da-DK")
            .UseMiddleware<WebAppMiddleware>(staticRuntimeEnvironment, templateFilePath);
    }

    private static string GetEnvironmentVariableOrThrow(string variableName)
    {
        return Environment.GetEnvironmentVariable(variableName)
               ?? throw new InvalidOperationException($"Required environment variable '{variableName}' is not set.");
    }

    private static string GetWebAppDistRoot(string webAppProjectName, string webAppDistRootName)
    {
        var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        var directoryInfo = new DirectoryInfo(assemblyPath);
        while (directoryInfo is not null &&
               directoryInfo.GetDirectories(webAppProjectName).Length == 0 &&
               !Path.Exists(Path.Join(directoryInfo.FullName, webAppProjectName, webAppDistRootName))
              )
        {
            directoryInfo = directoryInfo.Parent;
        }

        return Path.Join(directoryInfo!.FullName, webAppProjectName, webAppDistRootName);
    }
}