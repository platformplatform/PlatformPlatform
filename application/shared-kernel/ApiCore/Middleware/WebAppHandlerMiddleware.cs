using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace PlatformPlatform.SharedKernel.ApiCore.Middleware;

public class WebAppHandlerMiddleware : IMiddleware
{
    private const string PublicKeyPrefix = "PUBLIC_";
    private readonly string _html;
    private readonly string[] _publicAllowedKeys = {"CDN_URL"};

    public WebAppHandlerMiddleware(Dictionary<string, string> runtimeEnvironment, string htmlTemplate)
    {
        VerifyRuntimeEnvironment(runtimeEnvironment);
        _html = htmlTemplate.Replace("<ENCODED_RUNTIME_ENV>", EncodeRuntimeEnvironment(runtimeEnvironment));
    }

    public async Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
    {
        if (httpContext.Request.Path.ToString().EndsWith("/") == false)
        {
            await httpContext.Response.WriteAsync(_html);
        }
        else
        {
            await next(httpContext);
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