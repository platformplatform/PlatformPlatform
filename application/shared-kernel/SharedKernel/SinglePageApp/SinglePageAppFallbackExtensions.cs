using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.ExecutionContext;

namespace PlatformPlatform.SharedKernel.SinglePageApp;

public static class SinglePageAppFallbackExtensions
{
    public static IServiceCollection AddSinglePageAppFallback(
        this IServiceCollection services,
        params (string Key, string Value)[] environmentVariables
    )
    {
        return services.AddSingleton<SinglePageAppConfiguration>(serviceProvider =>
            {
                var environment = serviceProvider.GetRequiredService<IWebHostEnvironment>();
                return new SinglePageAppConfiguration(environment.IsDevelopment(), environmentVariables);
            }
        );
    }

    public static IApplicationBuilder UseSinglePageAppFallback(this WebApplication app)
    {
        app.Map("/remoteEntry.js", (HttpContext context, SinglePageAppConfiguration singlePageAppConfiguration) =>
            {
                SetResponseHttpHeaders(singlePageAppConfiguration, context.Response.Headers, "application/javascript");

                var javaScript = singlePageAppConfiguration.GetRemoteEntryJs();
                return context.Response.WriteAsync(javaScript);
            }
        );

        app.MapFallback((
                HttpContext context,
                IExecutionContext executionContext,
                IAntiforgery antiforgery,
                SinglePageAppConfiguration singlePageAppConfiguration
            ) =>
            {
                if (context.Request.Path.Value?.Contains("/api/", StringComparison.OrdinalIgnoreCase) == true ||
                    context.Request.Path.Value?.Contains("/internal-api/", StringComparison.OrdinalIgnoreCase) == true)
                {
                    context.Response.StatusCode = StatusCodes.Status404NotFound;
                    context.Response.ContentType = "text/plain";
                    return context.Response.WriteAsync("404 Not Found");
                }

                SetResponseHttpHeaders(singlePageAppConfiguration, context.Response.Headers, "text/html; charset=utf-8");

                var antiforgeryHttpHeaderToken = GenerateAntiforgeryTokens(antiforgery, context);

                var html = GetHtmlWithEnvironment(singlePageAppConfiguration, executionContext.UserInfo, antiforgeryHttpHeaderToken);

                return context.Response.WriteAsync(html);
            }
        );

        Directory.CreateDirectory(SinglePageAppConfiguration.BuildRootPath);

        return app
            .UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(SinglePageAppConfiguration.BuildRootPath) })
            .UseRequestLocalization(SinglePageAppConfiguration.SupportedLocalizations);
    }

    private static void SetResponseHttpHeaders(
        SinglePageAppConfiguration singlePageAppConfiguration,
        IHeaderDictionary responseHeaders,
        StringValues contentType
    )
    {
        // No cache headers
        responseHeaders.Append("Cache-Control", "no-cache, no-store, must-revalidate");
        responseHeaders.Append("Pragma", "no-cache");

        // Security policy headers
        responseHeaders.Append("X-Content-Type-Options", "nosniff");
        responseHeaders.Append("X-Frame-Options", "DENY");
        responseHeaders.Append("X-XSS-Protection", "1; mode=block");
        responseHeaders.Append("Referrer-Policy", "no-referrer, strict-origin-when-cross-origin");
        responseHeaders.Append("Permissions-Policy", singlePageAppConfiguration.PermissionPolicies);

        // Content security policy header
        responseHeaders.Append("Content-Security-Policy", singlePageAppConfiguration.ContentSecurityPolicies);

        // Content type header
        responseHeaders.Append("Content-Type", contentType);
    }

    private static string GenerateAntiforgeryTokens(IAntiforgery antiforgery, HttpContext context)
    {
        // ASP.NET Core antiforgery system uses a cryptographic double-submit pattern with two tokens:
        // - A secret cookie token that only the server can read (session-based)
        // - A public request token that the SPA sends as a header for state-changing requests like POST/PUT/DELETE

        var antiforgeryTokenSet = antiforgery.GetAndStoreTokens(context);

        if (antiforgeryTokenSet.CookieToken is not null)
        {
            // A new antiforgery cookie is only generated once, as it must remain constant across browser tabs to avoid validation failures
            context.Response.Cookies.Append(
                AuthenticationTokenHttpKeys.AntiforgeryTokenCookieName,
                antiforgeryTokenSet.CookieToken!,
                new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Path = "/" }
            );
        }

        return antiforgeryTokenSet.RequestToken!;
    }

    private static string GetHtmlWithEnvironment(
        SinglePageAppConfiguration singlePageAppConfiguration,
        UserInfo userInfo,
        string antiforgeryHttpHeaderToken
    )
    {
        var userInfoEncoded = JsonSerializer.Serialize(userInfo, SinglePageAppConfiguration.JsonHtmlEncodingOptions);

        // Escape the JSON for use in an HTML attribute
        var userInfoEscaped = HtmlEncoder.Default.Encode(userInfoEncoded);
        var html = singlePageAppConfiguration.GetHtmlTemplate();
        html = html.Replace("%ENCODED_RUNTIME_ENV%", singlePageAppConfiguration.StaticRuntimeEnvironmentEscaped);
        html = html.Replace("%ENCODED_USER_INFO_ENV%", userInfoEscaped);
        html = html.Replace("%LOCALE%", userInfo.Locale);
        html = html.Replace("%ANTIFORGERY_TOKEN%", antiforgeryHttpHeaderToken);

        foreach (var variable in singlePageAppConfiguration.StaticRuntimeEnvironment)
        {
            html = html.Replace($"%{variable.Key}%", variable.Value);
        }

        return html;
    }
}
