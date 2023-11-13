using System.Security;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace PlatformPlatform.SharedKernel.ApiCore.Middleware;

public class WebAppMiddleware
{
    public const string PublicUrlKey = "PUBLIC_URL";
    public const string CdnUrlKey = "CDN_URL";
    private const string PublicKeyPrefix = "PUBLIC_";

    private readonly StringValues _contentSecurityPolicy;
    private readonly string _html;
    private readonly RequestDelegate _next;
    private readonly string[] _publicAllowedKeys = {CdnUrlKey};

    public WebAppMiddleware(RequestDelegate next, Dictionary<string, string> runtimeEnvironment, string htmlTemplate)
    {
        VerifyRuntimeEnvironment(runtimeEnvironment);

        _next = next;

        var publicUrl = runtimeEnvironment.GetValueOrDefault(PublicUrlKey, "/") ?? "";
        var cdnUrl = runtimeEnvironment.GetValueOrDefault(CdnUrlKey, publicUrl) ?? "";
        var devServerWebsocket = cdnUrl.Replace("http", "wss");
        _html = htmlTemplate
            .Replace("<ENCODED_RUNTIME_ENV>", EncodeRuntimeEnvironment(runtimeEnvironment))
            .Replace("<PUBLIC_URL>", publicUrl)
            .Replace("<CDN_URL>", cdnUrl);

        var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "development";
        var trustedHosts = isDevelopment
            ? new[] {"'self'", publicUrl, cdnUrl, devServerWebsocket}
            : new[] {"'self'", publicUrl, cdnUrl};
        var contentSecurityPolicies = new Dictionary<string, string[]>
        {
            {
                "default-src", trustedHosts
            },
            {
                "connect-src", trustedHosts
            },
            {
                "script-src", trustedHosts
            }
        };

        _contentSecurityPolicy = string.Join(" ", contentSecurityPolicies
            .Select(policy =>
                $"{policy.Key} {string.Join(" ", policy.Value)};"
            ));
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        if (httpContext.Request.Path.ToString().EndsWith("/"))
        {
            httpContext.Response.Headers.Add("Content-Security-Policy", _contentSecurityPolicy);
            await httpContext.Response.WriteAsync(_html);
        }
        else
        {
            await _next(httpContext);
        }
    }

    private void VerifyRuntimeEnvironment(Dictionary<string, string> environmentVariables)
    {
        foreach (var variable in environmentVariables)
        {
            if (variable.Key.StartsWith(PublicKeyPrefix) || _publicAllowedKeys.Contains(variable.Key)) continue;

            throw new SecurityException($"Environment variable '{variable.Key}' is not allowed to be public.");
        }
    }

    private string EncodeRuntimeEnvironment(Dictionary<string, string> runtimeEnvironment)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(runtimeEnvironment)));
    }
}

public static class WebAppMiddlewareExtensions
{
    [UsedImplicitly]
    public static IApplicationBuilder UseWebAppMiddleware(this IApplicationBuilder builder,
        Dictionary<string, string>? publicEnvironmentVariables = null)
    {
        var runtimeEnvironmentVariables = new Dictionary<string, string?>
        {
            {WebAppMiddleware.PublicUrlKey, Environment.GetEnvironmentVariable(WebAppMiddleware.PublicUrlKey)},
            {WebAppMiddleware.CdnUrlKey, Environment.GetEnvironmentVariable(WebAppMiddleware.CdnUrlKey)}
        };

        if (publicEnvironmentVariables != null)
        {
            foreach (var variable in publicEnvironmentVariables)
            {
                runtimeEnvironmentVariables.Add(variable.Key, variable.Value);
            }
        }

        var solutionRootPath = Directory.GetParent(Environment.CurrentDirectory)!.FullName;
        var templateFilePath = Path.Join(solutionRootPath, "WebApp", "dist", "index.html");

        if (Path.Exists(templateFilePath) == false)
        {
            throw new FileNotFoundException("Could not find the index.html template file.", templateFilePath);
        }

        var template = File.ReadAllText(templateFilePath, new UTF8Encoding());

        return builder.UseMiddleware<WebAppMiddleware>(runtimeEnvironmentVariables, template);
    }
}