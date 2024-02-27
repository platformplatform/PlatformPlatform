using System.Security;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using PlatformPlatform.SharedKernel.InfrastructureCore;

namespace PlatformPlatform.SharedKernel.ApiCore.Middleware;

public sealed class WebAppMiddleware : IMiddleware
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
    private readonly string[] _publicAllowedKeys = [CdnUrlKey, ApplicationVersion];
    private readonly string _publicUrl;
    private readonly Dictionary<string, string> _staticRuntimeEnvironment;
    private string? _htmlTemplate;

    public WebAppMiddleware(
        IOptions<JsonOptions> jsonOptions,
        WebAppMiddlewareConfiguration configuration
    )
    {
        _htmlTemplatePath = configuration.HtmlTemplatePath;
        _jsonSerializerOptions = jsonOptions.Value.SerializerOptions;
        _staticRuntimeEnvironment = configuration.StaticRuntimeEnvironment;

        VerifyRuntimeEnvironment(_staticRuntimeEnvironment);

        _isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "development";
        _cdnUrl = _staticRuntimeEnvironment.GetValueOrDefault(CdnUrlKey)!;
        _publicUrl = _staticRuntimeEnvironment.GetValueOrDefault(PublicUrlKey)!;
    }

    public Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.Request.Path.ToString().StartsWith("/api/")) return next(context);

        var cultureFeature = context.Features.Get<IRequestCultureFeature>();
        var locale = cultureFeature?.RequestCulture.Culture.Name ?? "en-US";

        var userInfo = new UserInfo(context.User, locale);

        var requestEnvironmentVariables = new Dictionary<string, string> { { Locale, locale } };

        // Cache control
        ApplyNoCacheHeaders(context);
        // Security headers
        ApplySecurityPolicyHeaders(context);
        // Content security policy
        ApplyContentSecurityPolicy(context);
        // Set content type
        context.Response.Headers.Append("Content-Type", "text/html; charset=utf-8");

        return context.Response.WriteAsync(GetHtmlWithEnvironment(requestEnvironmentVariables, userInfo));
    }

    private void VerifyRuntimeEnvironment(Dictionary<string, string> environmentVariables)
    {
        foreach (var key in environmentVariables.Keys)
        {
            if (key.StartsWith(PublicKeyPrefix) || _publicAllowedKeys.Contains(key)) continue;

            throw new SecurityException($"Environment variable '{key}' is not allowed to be public.");
        }
    }

    private string GetHtmlWithEnvironment(Dictionary<string, string>? requestEnvironmentVariables, UserInfo userInfo)
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

        var encodedUserInfo = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(userInfo, _jsonSerializerOptions))
        );
        result = result.Replace("%ENCODED_USER_INFO_ENV%", encodedUserInfo);

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

    private void ApplyContentSecurityPolicy(HttpContext context)
    {
        var devServerWebsocket = _cdnUrl.Replace("https", "wss");

        string[] trustedHosts = _isDevelopment
            ? [_publicUrl, _cdnUrl, devServerWebsocket]
            : [_publicUrl, _cdnUrl];

        context.Response.Headers.Append("Content-Security-Policy", SerializeContentSecurityPolicy(
            new Dictionary<string, string[]>
            {
                { "script-src", trustedHosts.Concat(["'strict-dynamic'", "https:"]).ToArray() },
                { "script-src-elem", trustedHosts },
                { "default-src", trustedHosts },
                { "connect-src", trustedHosts },
                { "img-src", trustedHosts.Append("data:").ToArray() },
                { "object-src", ["'none'"] },
                { "base-uri", ["'none'"] }
                // { "require-trusted-types-for", ["'script'"] }
            }));
    }

    private static void ApplySecurityPolicyHeaders(HttpContext context)
    {
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Append("Referrer-Policy", "no-referrer, strict-origin-when-cross-origin");
        context.Response.Headers.Append("Permissions-Policy", SerializePermissionsPolicy(
            new Dictionary<string, string[]>
            {
                { "geolocation", [] },
                { "microphone", [] },
                { "camera", [] },
                { "picture-in-picture", [] },
                { "display-capture", [] },
                { "fullscreen", [] },
                { "web-share", [] },
                { "identity-credentials-get", [] }
            }));
    }

    private static string SerializePermissionsPolicy(Dictionary<string, string[]> permissionsPolicy)
    {
        return string.Join(", ", permissionsPolicy.Select(p => $"{p.Key}=({string.Join(", ", p.Value)})"));
    }

    private static string SerializeContentSecurityPolicy(Dictionary<string, string[]> contentSecurityPolicies)
    {
        return string.Join(" ", contentSecurityPolicies.Select(p => $"{p.Key} {string.Join(" ", p.Value)};"));
    }
}

public static class WebAppMiddlewareExtensions
{
    [UsedImplicitly]
    public static IApplicationBuilder UseWebAppMiddleware(
        this IApplicationBuilder builder
    )
    {
        if (InfrastructureCoreConfiguration.SwaggerGenerator) return builder;

        var configuration = builder.ApplicationServices.GetRequiredService<WebAppMiddlewareConfiguration>();

        return builder
            .UseStaticFiles(new StaticFileOptions
                { FileProvider = new PhysicalFileProvider(configuration.BuildRootPath) })
            .UseRequestLocalization("en-US", "da-DK")
            .UseMiddleware<WebAppMiddleware>();
    }

    [UsedImplicitly]
    public static IServiceCollection AddWebAppMiddleware(this IServiceCollection services)
    {
        return services.AddWebAppMiddleware(_ => { });
    }

    [UsedImplicitly]
    public static IServiceCollection AddWebAppMiddleware(
        this IServiceCollection services,
        Action<WebAppMiddlewareConfiguration> configureOptions
    )
    {
        if (InfrastructureCoreConfiguration.SwaggerGenerator) return services;

        var configuration = new WebAppMiddlewareConfiguration();

        configureOptions.Invoke(configuration);

        return services
            .AddSingleton(configuration)
            .AddTransient<WebAppMiddleware>();
    }
}

[UsedImplicitly]
public class WebAppMiddlewareConfiguration
{
    public WebAppMiddlewareConfiguration(
        string webAppProjectName = "WebApp",
        Dictionary<string, string>? publicEnvironmentVariables = null
    )
    {
        var publicUrl = GetEnvironmentVariableOrThrow(WebAppMiddleware.PublicUrlKey);
        var cdnUrl = GetEnvironmentVariableOrThrow(WebAppMiddleware.CdnUrlKey);
        var applicationVersion = Assembly.GetEntryAssembly()!.GetName().Version!.ToString();

        var environmentVariables = new Dictionary<string, string>
        {
            { WebAppMiddleware.PublicUrlKey, publicUrl },
            { WebAppMiddleware.CdnUrlKey, cdnUrl },
            { WebAppMiddleware.ApplicationVersion, applicationVersion }
        };

        StaticRuntimeEnvironment = publicEnvironmentVariables is null
            ? environmentVariables
            : environmentVariables.Concat(publicEnvironmentVariables).ToDictionary();

        BuildRootPath = GetWebAppDistRoot(webAppProjectName, "dist");
        HtmlTemplatePath = Path.Combine(BuildRootPath, "index.html");

        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "development")
        {
            for (var i = 0; i < 10; i++)
            {
                if (File.Exists(HtmlTemplatePath)) break;
                Debug.WriteLine($"Waiting for {webAppProjectName} build to be ready...");
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }

        if (!File.Exists(HtmlTemplatePath))
        {
            throw new FileNotFoundException("index.html does not exist.", HtmlTemplatePath);
        }
    }

    public Dictionary<string, string> StaticRuntimeEnvironment { get; }

    public string HtmlTemplatePath { get; }

    public string BuildRootPath { get; }

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

[UsedImplicitly(ImplicitUseTargetFlags.Members)]
public class UserInfo
{
    public UserInfo(ClaimsPrincipal user, string defaultLocale)
    {
        IsAuthenticated = user.Identity?.IsAuthenticated ?? false;
        Locale = user.FindFirst("locale")?.Value ?? defaultLocale;

        if (IsAuthenticated)
        {
            Email = user.Identity?.Name;
            Name = user.FindFirst(ClaimTypes.Name)?.Value;
            Role = user.FindFirst(ClaimTypes.Role)?.Value;
            TenantId = user.FindFirst("tenantId")?.Value;
        }
    }

    public bool IsAuthenticated { get; init; }

    public string Locale { get; init; }

    public string? Email { get; init; }

    public string? Name { get; init; }

    public string? Role { get; init; }

    public string? TenantId { get; init; }
}