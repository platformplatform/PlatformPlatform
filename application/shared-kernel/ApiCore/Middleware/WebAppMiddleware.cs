using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;

namespace PlatformPlatform.SharedKernel.ApiCore.Middleware;

public sealed class WebAppMiddleware(
    IOptions<JsonOptions> jsonOptions,
    WebAppMiddlewareConfiguration webAppConfiguration
)
    : IMiddleware
{
    public Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.Request.Path.ToString().StartsWith("/api/")) return next(context);
        
        SetResponseHttpHeaders(context.Response.Headers);
        
        var defaultLocale = context.Features.Get<IRequestCultureFeature>()?.RequestCulture.Culture.Name ?? "en-US";
        var userInfo = new UserInfo(context.User, defaultLocale);
        var html = GetHtmlWithEnvironment(userInfo);
        return context.Response.WriteAsync(html);
    }
    
    private void SetResponseHttpHeaders(IHeaderDictionary responseHeaders)
    {
        // No cache headers
        responseHeaders.Append("Cache-Control", "no-cache, no-store, must-revalidate");
        responseHeaders.Append("Pragma", "no-cache");
        
        // Security policy headers
        responseHeaders.Append("X-Content-Type-Options", "nosniff");
        responseHeaders.Append("X-Frame-Options", "DENY");
        responseHeaders.Append("X-XSS-Protection", "1; mode=block");
        responseHeaders.Append("Referrer-Policy", "no-referrer, strict-origin-when-cross-origin");
        responseHeaders.Append("Permissions-Policy", webAppConfiguration.PermissionPolicies);
        
        // Content security policy header
        responseHeaders.Append("Content-Security-Policy", webAppConfiguration.ContentSecurityPolicies);
        
        // Content type header
        responseHeaders.Append("Content-Type", "text/html; charset=utf-8");
    }
    
    private string GetHtmlWithEnvironment(UserInfo userInfo)
    {
        var encodedUserInfo = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(userInfo, jsonOptions.Value.SerializerOptions))
        );
        
        var html = webAppConfiguration.GetHtmlTemplate();
        html = html.Replace("%ENCODED_RUNTIME_ENV%", webAppConfiguration.StaticRuntimeEnvironmentEncoded);
        html = html.Replace("%ENCODED_USER_INFO_ENV%", encodedUserInfo);
        html = html.Replace("%LOCALE%", userInfo.Locale);
        
        foreach (var variable in webAppConfiguration.StaticRuntimeEnvironment)
        {
            html = html.Replace($"%{variable.Key}%", variable.Value);
        }
        
        return html;
    }
}
