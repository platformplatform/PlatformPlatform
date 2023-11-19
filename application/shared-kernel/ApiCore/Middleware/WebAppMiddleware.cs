using System.Security;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace PlatformPlatform.SharedKernel.ApiCore.Middleware;

public class WebAppMiddleware
{
    private const string PublicKeyPrefix = "PUBLIC_";
    public const string PublicUrlKey = "PUBLIC_URL";
    public const string CdnUrlKey = "CDN_URL";
    public const string ApplicationVersion = "APPLICATION_VERSION";

    private readonly string _cdnUrl;
    private readonly StringValues _contentSecurityPolicy;
    private readonly string _htmlTemplatePath;
    private readonly bool _isDevelopment;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly RequestDelegate _next;
    private readonly string[] _publicAllowedKeys = { CdnUrlKey, ApplicationVersion };
    private readonly string _publicUrl;
    private readonly Dictionary<string, string> _runtimeEnvironment;
    private string? _htmlTemplate;

    public WebAppMiddleware(RequestDelegate next, Dictionary<string, string> runtimeEnvironment,
        string htmlTemplatePath, IOptions<JsonOptions> jsonOptions)
    {
        _next = next;
        _runtimeEnvironment = runtimeEnvironment;
        _htmlTemplatePath = htmlTemplatePath;
        _jsonSerializerOptions = jsonOptions.Value.SerializerOptions;

        VerifyRuntimeEnvironment(runtimeEnvironment);

        _isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "development";
        _cdnUrl = runtimeEnvironment.GetValueOrDefault(CdnUrlKey)!;
        _publicUrl = runtimeEnvironment.GetValueOrDefault(PublicUrlKey)!;
        _contentSecurityPolicy = GetContentSecurityPolicy();
    }

    private void VerifyRuntimeEnvironment(Dictionary<string, string> environmentVariables)
    {
        foreach (var key in environmentVariables.Keys)
        {
            if (key.StartsWith(PublicKeyPrefix) || _publicAllowedKeys.Contains(key)) continue;

            throw new SecurityException($"Environment variable '{key}' is not allowed to be public.");
        }
    }

    private string GetHtmlWithEnvironment()
    {
        if (_htmlTemplate == null || _isDevelopment)
        {
            _htmlTemplate = File.ReadAllText(_htmlTemplatePath, new UTF8Encoding());
        }

        var encodeRuntimeEnvironment = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(_runtimeEnvironment, _jsonSerializerOptions))
        );
        var result = _htmlTemplate.Replace("<ENCODED_RUNTIME_ENV>", encodeRuntimeEnvironment);

        foreach (var variable in _runtimeEnvironment)
        {
            result = result.Replace($"<{variable.Key}>", variable.Value);
        }

        return result;
    }

    private StringValues GetContentSecurityPolicy()
    {
        var devServerWebsocket = _cdnUrl.Replace("http", "wss");

        var trustedHosts = _isDevelopment
            ? new[] { "'self'", _publicUrl, _cdnUrl, devServerWebsocket }
            : new[] { "'self'", _publicUrl, _cdnUrl };

        var contentSecurityPolicies = new Dictionary<string, string[]>
        {
            { "default-src", trustedHosts },
            { "connect-src", trustedHosts },
            { "script-src", trustedHosts }
        };

        return string.Join(
            " ",
            contentSecurityPolicies.Select(policy => $"{policy.Key} {string.Join(" ", policy.Value)};"
            )
        );
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.ToString().EndsWith("/"))
        {
            context.Response.Headers.Add("Content-Security-Policy", _contentSecurityPolicy);
            await context.Response.WriteAsync(GetHtmlWithEnvironment());
        }
        else
        {
            await _next(context);
        }
    }
}

public static class WebAppMiddlewareExtensions
{
    [UsedImplicitly]
    public static IApplicationBuilder UseWebAppMiddleware(this IApplicationBuilder builder,
        string webAppProjectName = "WebApp", Dictionary<string, string>? publicEnvironmentVariables = null)
    {
        if (Environment.GetEnvironmentVariable("SWAGGER_GENERATOR") == "true") return builder;

        var publicUrl = GetEnvironmentVariableOrThrow(WebAppMiddleware.PublicUrlKey);
        var cdnUrl = GetEnvironmentVariableOrThrow(WebAppMiddleware.CdnUrlKey);
        var applicationVersion = Assembly.GetEntryAssembly()!.GetName().Version!.ToString();

        var runtimeEnvironmentVariables = new Dictionary<string, string>
        {
            { WebAppMiddleware.PublicUrlKey, publicUrl },
            { WebAppMiddleware.CdnUrlKey, cdnUrl },
            { WebAppMiddleware.ApplicationVersion, applicationVersion }
        };

        if (publicEnvironmentVariables != null)
        {
            foreach (var variable in publicEnvironmentVariables)
            {
                runtimeEnvironmentVariables.Add(variable.Key, variable.Value);
            }
        }

        var buildRootPath = GetWebAppDistRoot(webAppProjectName, "dist");
        var templateFilePath = Path.Combine(buildRootPath, "index.html");

        if (!File.Exists(templateFilePath))
        {
            throw new FileNotFoundException("index.html does not exist.", templateFilePath);
        }

        return builder
            .UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(buildRootPath) })
            .UseMiddleware<WebAppMiddleware>(runtimeEnvironmentVariables, templateFilePath);
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
        while (directoryInfo is not null && !directoryInfo.GetDirectories(webAppProjectName).Any() &&
               !Path.Exists(Path.Join(directoryInfo.FullName, webAppProjectName, webAppDistRootName)))
        {
            directoryInfo = directoryInfo.Parent;
        }

        return Path.Join(directoryInfo!.FullName, webAppProjectName, webAppDistRootName);
    }
}