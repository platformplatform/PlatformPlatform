using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace PlatformPlatform.SharedKernel.ApiCore.SinglePageApp;

public static class SinglePageAppFallbackExtensions
{
    public static IServiceCollection AddSinglePageAppFallback(this IServiceCollection services)
    {
        return services.AddSingleton<SinglePageAppConfiguration>(serviceProvider =>
            {
                var jsonOptions = serviceProvider.GetRequiredService<IOptions<JsonOptions>>();
                var environment = serviceProvider.GetRequiredService<IWebHostEnvironment>();
                return new SinglePageAppConfiguration(jsonOptions, environment.IsDevelopment());
            }
        );
    }

    public static IApplicationBuilder UseSinglePageAppFallback(this WebApplication app)
    {
        app.MapFallback((HttpContext context, IOptions<JsonOptions> jsonOptions, SinglePageAppConfiguration singlePageAppConfiguration) =>
            {
                SetResponseHttpHeaders(singlePageAppConfiguration, context.Response.Headers);

                var defaultLocale = context.Features.Get<IRequestCultureFeature>()?.RequestCulture.Culture.Name ?? "en-US";
                var userInfo = new UserInfo(context.User, defaultLocale);
                var html = GetHtmlWithEnvironment(singlePageAppConfiguration, userInfo, jsonOptions);
                return context.Response.WriteAsync(html);
            }
        );

        Directory.CreateDirectory(SinglePageAppConfiguration.BuildRootPath);

        return app
            .UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(SinglePageAppConfiguration.BuildRootPath) })
            .UseRequestLocalization("en-US", "da-DK");
    }

    private static void SetResponseHttpHeaders(SinglePageAppConfiguration singlePageAppConfiguration, IHeaderDictionary responseHeaders)
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
        responseHeaders.Append("Content-Type", "text/html; charset=utf-8");
    }

    private static string GetHtmlWithEnvironment(SinglePageAppConfiguration singlePageAppConfiguration, UserInfo userInfo, IOptions<JsonOptions> jsonOptions)
    {
        var encodedUserInfo = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(userInfo, jsonOptions.Value.SerializerOptions))
        );

        var html = singlePageAppConfiguration.GetHtmlTemplate();
        html = html.Replace("%ENCODED_RUNTIME_ENV%", singlePageAppConfiguration.StaticRuntimeEnvironmentEncoded);
        html = html.Replace("%ENCODED_USER_INFO_ENV%", encodedUserInfo);
        html = html.Replace("%LOCALE%", userInfo.Locale);

        foreach (var variable in singlePageAppConfiguration.StaticRuntimeEnvironment)
        {
            html = html.Replace($"%{variable.Key}%", variable.Value);
        }

        return html;
    }
}
