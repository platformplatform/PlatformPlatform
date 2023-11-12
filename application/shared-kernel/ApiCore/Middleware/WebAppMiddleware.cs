using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace PlatformPlatform.SharedKernel.ApiCore.Middleware;

public class WebAppMiddleware
{
    private const string PublicKeyPrefix = "PUBLIC_";
    private readonly string _contentSecurityPolicy;
    private readonly string _html;
    private readonly RequestDelegate _next;
    private readonly string[] _publicAllowedKeys = {"CDN_URL"};

    public WebAppMiddleware(RequestDelegate next, Dictionary<string, string> runtimeEnvironment, string htmlTemplate)
    {
        VerifyRuntimeEnvironment(runtimeEnvironment);
        var publicUrl = runtimeEnvironment.GetValueOrDefault("PUBLIC_URL", "/");
        var cdnUrl = runtimeEnvironment.GetValueOrDefault("CDN_URL", publicUrl);
        _next = next;
        _html = htmlTemplate
            .Replace("<ENCODED_RUNTIME_ENV>", EncodeRuntimeEnvironment(runtimeEnvironment))
            .Replace("<PUBLIC_URL>", publicUrl)
            .Replace("<CDN_URL>", cdnUrl);
        // todo: Clean up CSP
        _contentSecurityPolicy =
            $"default-src 'self' {publicUrl} {cdnUrl} {cdnUrl.Replace("http", "wss")};" +
            $"script-src 'self' {publicUrl} {cdnUrl} {cdnUrl.Replace("http", "wss")};" +
            $"connect-src 'self' {publicUrl} {cdnUrl} {cdnUrl.Replace("http", "wss")}/ws;";
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
            if (variable.Key.StartsWith(PublicKeyPrefix) || _publicAllowedKeys.Contains(variable.Key))
            {
                continue;
            }

            throw new Exception($"Security: Environment variable \"{variable.Key}\" is not allowed to be public");
        }
    }

    private static string EncodeRuntimeEnvironment(Dictionary<string, string> runtimeEnvironment)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(runtimeEnvironment)));
    }
}

public static class WebAppMiddlewareExtensions
{
    public static IApplicationBuilder UseWebApp(this IApplicationBuilder builder,
        Dictionary<string, string?>? publicEnvironmentVariables = null)
    {
        var runTimeEnv = new Dictionary<string, string?>
        {
            {"PUBLIC_URL", Environment.GetEnvironmentVariable("PUBLIC_URL")},
            {"CDN_URL", Environment.GetEnvironmentVariable("CDN_URL")}
        };

        if (publicEnvironmentVariables != null)
        {
            foreach (var variable in publicEnvironmentVariables)
            {
                runTimeEnv.Add(variable.Key, variable.Value);
            }
        }

        var systemRootPath = Directory.GetParent(Environment.CurrentDirectory)!.FullName;
        var templateFilePath = Path.Join(systemRootPath, "WebApp", "dist", "index.html");

        if (Path.Exists(templateFilePath) == false)
        {
            throw new FileNotFoundException($"Error: Could not find client html template \"{templateFilePath}\"");
        }

        var template = File.ReadAllText(templateFilePath, new UTF8Encoding());

        return builder.UseMiddleware<WebAppMiddleware>(runTimeEnv, template);
    }
}