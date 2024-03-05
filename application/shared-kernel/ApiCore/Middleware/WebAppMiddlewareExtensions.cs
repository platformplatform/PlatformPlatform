using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using PlatformPlatform.SharedKernel.InfrastructureCore;

namespace PlatformPlatform.SharedKernel.ApiCore.Middleware;

public static class WebAppMiddlewareExtensions
{
    [UsedImplicitly]
    public static IServiceCollection AddWebAppMiddleware(this IServiceCollection services)
    {
        if (InfrastructureCoreConfiguration.SwaggerGenerator) return services;

        return services
            .AddSingleton(new WebAppMiddlewareConfiguration())
            .AddTransient<WebAppMiddleware>();
    }

    [UsedImplicitly]
    public static IApplicationBuilder UseWebAppMiddleware(this IApplicationBuilder builder)
    {
        if (InfrastructureCoreConfiguration.SwaggerGenerator) return builder;

        var webAppConfiguration = builder.ApplicationServices.GetRequiredService<WebAppMiddlewareConfiguration>();

        return builder
            .UseStaticFiles(new StaticFileOptions
                { FileProvider = new PhysicalFileProvider(webAppConfiguration.BuildRootPath) })
            .UseRequestLocalization("en-US", "da-DK")
            .UseMiddleware<WebAppMiddleware>();
    }
}