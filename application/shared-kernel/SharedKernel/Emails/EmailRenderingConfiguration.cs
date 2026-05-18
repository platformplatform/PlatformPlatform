using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Scriban.Runtime;
using SharedKernel.Configuration;
using SharedKernel.SinglePageApp;

namespace SharedKernel.Emails;

public static class EmailRenderingConfiguration
{
    // Path segment under which email assets (images, fonts, etc.) referenced from rendered HTML
    // are served. Templates author absolute URLs like {{publicUrl}}/emails/assets/logo.png so the
    // same markup works in dev (localhost) and production (CDN/public host).
    public const string EmailStaticFilesRequestPath = "/emails/assets";

    private static string ResolveEmailsDistPath(string webAppProjectName)
    {
        // Walk up from the executing assembly looking for <webAppProjectName>/emails/dist or its
        // parent <webAppProjectName>/main.tsx marker. Mirrors SinglePageAppConfiguration so the
        // same SCS layout works for both SPA bundles and email bundles.
        var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var directoryInfo = new DirectoryInfo(assemblyPath);
        while (directoryInfo is not null)
        {
            var candidate = Path.Combine(directoryInfo.FullName, webAppProjectName, "emails", "dist");
            if (Directory.Exists(candidate)) return candidate;

            var webAppCandidate = Path.Combine(directoryInfo.FullName, webAppProjectName);
            if (File.Exists(Path.Combine(webAppCandidate, "main.tsx"))) return Path.Combine(webAppCandidate, "emails", "dist");

            directoryInfo = directoryInfo.Parent;
        }

        throw new InvalidOperationException($"Could not locate the WebApp project '{webAppProjectName}' walking up from '{assemblyPath}'.");
    }

    extension(IServiceCollection services)
    {
        public IServiceCollection AddEmailRendering(string webAppProjectName)
        {
            var emailsDistPath = ResolveEmailsDistPath(webAppProjectName);
            var publicUrl = Environment.GetEnvironmentVariable(SinglePageAppConfiguration.PublicUrlKey) ?? string.Empty;

            services.AddSingleton<ScriptObject>(_ => EmailHelpers.CreateScriptObject(publicUrl));

            services.AddSingleton<IEmailTemplateLoader>(_ => new FileSystemEmailTemplateLoader(emailsDistPath, !SharedInfrastructureConfiguration.IsRunningInAzure));
            services.AddSingleton<IEmailRenderer, ScribanEmailRenderer>();

            return services;
        }
    }

    extension(IApplicationBuilder app)
    {
        // `includePreviewArtifacts` controls whether the *.preview.html/.txt files are reachable.
        // Those are dev-tooling renders (sample data, no secrets) consumed only by the back-office
        // email preview page: served on the auth-gated back-office host, 404'd on the public host
        // so they are not publicly fetchable by guessing the path.
        //
        // Preview artifacts keep an unsubstituted {{PublicUrl}} placeholder (the email build no
        // longer bakes it). When previews are served it is resolved here to `appPublicUrl` -- the
        // always-on public app host -- so the preview's logo and links point exactly where a real
        // email loads them, with nothing email-related hosted on the back-office.
        public IApplicationBuilder UseEmailStaticFiles(
            string webAppProjectName,
            bool includePreviewArtifacts,
            string? appPublicUrl = null
        )
        {
            var distRoot = Path.GetFullPath(ResolveEmailsDistPath(webAppProjectName));
            if (!Directory.Exists(distRoot)) Directory.CreateDirectory(distRoot);

            app.Use(async (context, next) =>
                {
                    var path = context.Request.Path.Value;
                    var isPreviewArtifact = path is not null
                                            && path.StartsWith(EmailStaticFilesRequestPath, StringComparison.OrdinalIgnoreCase)
                                            && (path.EndsWith(".preview.html", StringComparison.OrdinalIgnoreCase)
                                                || path.EndsWith(".preview.txt", StringComparison.OrdinalIgnoreCase));

                    if (!isPreviewArtifact)
                    {
                        await next();
                        return;
                    }

                    if (!includePreviewArtifacts)
                    {
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        return;
                    }

                    var relativePath = path![EmailStaticFilesRequestPath.Length..].TrimStart('/');
                    var fullPath = Path.GetFullPath(Path.Combine(distRoot, relativePath));
                    if (!fullPath.StartsWith(distRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                        || !File.Exists(fullPath))
                    {
                        context.Response.StatusCode = StatusCodes.Status404NotFound;
                        return;
                    }

                    var publicUrl = (appPublicUrl ?? string.Empty).TrimEnd('/');
                    var content = (await File.ReadAllTextAsync(fullPath))
                        .Replace("{{PublicUrl}}", publicUrl)
                        .Replace("{{ PublicUrl }}", publicUrl);
                    context.Response.ContentType = path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)
                        ? "text/plain; charset=utf-8"
                        : "text/html; charset=utf-8";
                    await context.Response.WriteAsync(content);
                }
            );

            return app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(distRoot),
                    RequestPath = EmailStaticFilesRequestPath
                }
            );
        }
    }
}
