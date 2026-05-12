using System.Security.Cryptography;
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
using SharedKernel.Authentication;
using SharedKernel.Configuration;
using SharedKernel.ExecutionContext;

namespace SharedKernel.SinglePageApp;

public static class SinglePageAppFallbackExtensions
{
    private static void SetResponseHttpHeaders(
        SinglePageAppConfiguration singlePageAppConfiguration,
        IHeaderDictionary responseHeaders,
        StringValues contentType,
        string nonce
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
        var contentSecurityPolicy = singlePageAppConfiguration.ContentSecurityPolicies.Replace("{NONCE_PLACEHOLDER}", nonce);
        responseHeaders.Append("Content-Security-Policy", contentSecurityPolicy);

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
        string antiforgeryHttpHeaderToken,
        string nonce
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
        html = html.Replace("%CSP_NONCE%", nonce);
        html = html.Replace("{{cspNonce}}", nonce);

        foreach (var variable in singlePageAppConfiguration.StaticRuntimeEnvironment)
        {
            html = html.Replace($"%{variable.Key}%", variable.Value);
        }

        return html;
    }

    private static SinglePageAppConfiguration BuildConfiguration(HostScopedSinglePageApp singlePageApp, IWebHostEnvironment environment)
    {
        var configuration = new SinglePageAppConfiguration(
            environment.IsDevelopment(),
            singlePageApp.EnvironmentVariables,
            singlePageApp.WebAppProjectName,
            singlePageApp.PublicUrl,
            singlePageApp.CdnUrl
        );

        Directory.CreateDirectory(configuration.BundleDirectory);
        return configuration;
    }

    // In Azure, rsbuild has already copied the SPA's public/ assets into BundleDirectory at build time,
    // so the bundle provider serves them directly. In local dev, rsbuild's dev server holds public/
    // assets and never materializes them into BundleDirectory, leading to 404s on /manifest.json,
    // /favicon.ico, etc. Layering the public/ directory underneath the bundle provider closes that
    // gap without per-asset rules in BackOfficeDevStaticProxy.
    private static IFileProvider BuildSpaFileProvider(SinglePageAppConfiguration configuration)
    {
        var bundleProvider = new PhysicalFileProvider(configuration.BundleDirectory);
        if (SharedInfrastructureConfiguration.IsRunningInAzure
            || configuration.PublicDirectory is null
            || !Directory.Exists(configuration.PublicDirectory))
        {
            return bundleProvider;
        }

        return new CompositeFileProvider(bundleProvider, new PhysicalFileProvider(configuration.PublicDirectory));
    }

    private static void RegisterSpaEndpoints(
        WebApplication app,
        HostScopedSinglePageApp spa,
        SinglePageAppConfiguration configuration,
        bool gateByHost
    )
    {
        var remoteEntry = app.MapGet("/remoteEntry.js", context =>
            {
                var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));

                SetResponseHttpHeaders(configuration, context.Response.Headers, "application/javascript", nonce);

                var javaScript = configuration.GetRemoteEntryJs();
                return context.Response.WriteAsync(javaScript);
            }
        );

        // Catch-alls for unmatched API paths so /api/account/* on the back-office host (and vice versa)
        // surface as 404 rather than falling through to the SPA fallback and rendering the shell.
        var apiCatchAll = app.MapMethods("/api/{**_}", ["GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS"], context =>
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                context.Response.ContentType = "text/plain";
                return context.Response.WriteAsync("404 Not Found");
            }
        );
        var internalApiCatchAll = app.MapMethods("/internal-api/{**_}", ["GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS"], context =>
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                context.Response.ContentType = "text/plain";
                return context.Response.WriteAsync("404 Not Found");
            }
        );

        Task RenderShell(HttpContext context, IAntiforgery antiforgery)
        {
            var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));

            SetResponseHttpHeaders(configuration, context.Response.Headers, "text/html; charset=utf-8", nonce);

            var antiforgeryHttpHeaderToken = GenerateAntiforgeryTokens(antiforgery, context);

            var userInfo = spa.UserInfoFactory(context);
            var html = GetHtmlWithEnvironment(configuration, userInfo, antiforgeryHttpHeaderToken, nonce);

            return context.Response.WriteAsync(html);
        }

        // Unauthenticated SPA-shell routes registered as higher-priority GETs so the auth-gated
        // fallback below does not challenge them. Used for paths the SPA itself must serve while
        // the user is anonymous (e.g. development-only mock-login picker on the back-office host).
        var unauthenticatedRoutes = spa.UnauthenticatedPaths
            .Select(path => app.MapGet(path, RenderShell))
            .ToArray();

        var fallback = app.MapFallback(RenderShell);

        if (gateByHost)
        {
            remoteEntry.RequireHost(spa.Host);
            apiCatchAll.RequireHost(spa.Host);
            internalApiCatchAll.RequireHost(spa.Host);
            foreach (var route in unauthenticatedRoutes)
            {
                route.RequireHost(spa.Host);
            }

            fallback.RequireHost(spa.Host);
        }

        // When the SPA carries an authorization policy, the SPA-shell request must run through the
        // auth pipeline before UserInfoFactory inspects HttpContext.User. Without it, the back-office
        // shell renders an anonymous principal and the SSR-injected userInfoEnv is wrong.
        if (spa.AuthorizationPolicy is not null)
        {
            fallback.RequireAuthorization(spa.AuthorizationPolicy);
        }
    }

    extension(IServiceCollection services)
    {
        public IServiceCollection AddSinglePageAppFallback(Dictionary<string, string>? environmentVariables = null)
        {
            return services.AddSingleton<SinglePageAppConfiguration>(serviceProvider =>
                {
                    var environment = serviceProvider.GetRequiredService<IWebHostEnvironment>();
                    return new SinglePageAppConfiguration(environment.IsDevelopment(), environmentVariables);
                }
            );
        }
    }

    extension(WebApplication app)
    {
        public IApplicationBuilder UseFederatedModuleStaticFiles()
        {
            Directory.CreateDirectory(SinglePageAppConfiguration.BuildRootPath);

            return app
                .UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(SinglePageAppConfiguration.BuildRootPath) });
        }

        public IApplicationBuilder UseSinglePageAppFallback()
        {
            app.Map("/remoteEntry.js", (HttpContext context, SinglePageAppConfiguration singlePageAppConfiguration) =>
                {
                    var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));

                    SetResponseHttpHeaders(singlePageAppConfiguration, context.Response.Headers, "application/javascript", nonce);

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

                    var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));

                    SetResponseHttpHeaders(singlePageAppConfiguration, context.Response.Headers, "text/html; charset=utf-8", nonce);

                    var antiforgeryHttpHeaderToken = GenerateAntiforgeryTokens(antiforgery, context);

                    var html = GetHtmlWithEnvironment(singlePageAppConfiguration, executionContext.UserInfo, antiforgeryHttpHeaderToken, nonce);

                    return context.Response.WriteAsync(html);
                }
            );

            Directory.CreateDirectory(SinglePageAppConfiguration.BuildRootPath);

            var bundleProvider = new PhysicalFileProvider(SinglePageAppConfiguration.BuildRootPath);
            var fileProvider = !SharedInfrastructureConfiguration.IsRunningInAzure
                               && SinglePageAppConfiguration.PublicRootPath is not null
                               && Directory.Exists(SinglePageAppConfiguration.PublicRootPath)
                ? (IFileProvider)new CompositeFileProvider(bundleProvider, new PhysicalFileProvider(SinglePageAppConfiguration.PublicRootPath))
                : bundleProvider;

            return app
                .UseStaticFiles(new StaticFileOptions { FileProvider = fileProvider })
                .UseRequestLocalization(SinglePageAppConfiguration.SupportedLocalizations);
        }

        // Registers one MapFallback per host-scoped SPA so a single process can serve multiple SPAs
        // bound to different hostnames (e.g. local Aspire hosting account/WebApp on app.dev.localhost
        // and account/BackOffice on back-office.dev.localhost via dual Kestrel listeners). Each entry
        // serves its own bundle directory, embeds its own userInfo, and is restricted via RequireHost.
        // In production, prefer UseSingleSpaFallback: each container app runs the same image but only
        // serves one SPA, removing the dependency on Request.Host being correctly rewritten from
        // X-Forwarded-Host through the ACA mesh.
        public IApplicationBuilder UseHostScopedSinglePageAppFallback(params HostScopedSinglePageApp[] singlePageApps)
        {
            if (singlePageApps.Length == 0)
            {
                throw new ArgumentException("At least one host-scoped SPA must be provided.", nameof(singlePageApps));
            }

            var environment = app.Services.GetRequiredService<IWebHostEnvironment>();

            foreach (var singlePageApp in singlePageApps)
            {
                var configuration = BuildConfiguration(singlePageApp, environment);
                var fileProvider = BuildSpaFileProvider(configuration);

                RegisterSpaEndpoints(app, singlePageApp, configuration, true);

                // Host-scope the static-file middleware so each SPA only serves assets from its own bundle
                // directory on its own host. Without this, requests like https://back-office.../legal/terms.md
                // would fall through to the user-facing SPA's static files (which bake legal docs into dist),
                // leaking content the back-office bundle does not contain.
                var spa = singlePageApp;
                app.UseWhen(
                    context => context.Request.Host.Host.Equals(spa.Host, StringComparison.OrdinalIgnoreCase),
                    branch => branch.UseStaticFiles(new StaticFileOptions { FileProvider = fileProvider })
                );
            }

            return app.UseRequestLocalization(SinglePageAppConfiguration.SupportedLocalizations);
        }

        // Production-side counterpart to UseHostScopedSinglePageAppFallback: registers a single SPA on
        // a container that only ever serves one SPA externally. No RequireHost gate, so endpoint
        // matching works regardless of Request.Host (which in ACA arrives as the internal cluster FQDN
        // unless every layer of forwarded-headers trust is correctly configured). The same image runs
        // in both the account-api and back-office Container Apps; an env var picks which SPA each one
        // registers (see Account.Api Program.cs).
        public IApplicationBuilder UseSingleSpaFallback(HostScopedSinglePageApp singlePageApp)
        {
            var environment = app.Services.GetRequiredService<IWebHostEnvironment>();
            var configuration = BuildConfiguration(singlePageApp, environment);
            var fileProvider = BuildSpaFileProvider(configuration);

            RegisterSpaEndpoints(app, singlePageApp, configuration, false);

            app.UseStaticFiles(new StaticFileOptions { FileProvider = fileProvider });

            return app.UseRequestLocalization(SinglePageAppConfiguration.SupportedLocalizations);
        }
    }
}
