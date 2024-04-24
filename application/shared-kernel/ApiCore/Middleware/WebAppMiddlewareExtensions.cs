using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace PlatformPlatform.SharedKernel.ApiCore.Middleware;

public static class WebAppMiddlewareExtensions
{
    public static IServiceCollection AddWebAppMiddleware(this IServiceCollection services)
    {
        return services.AddSingleton<WebAppMiddlewareConfiguration>(serviceProvider =>
                {
                    var jsonOptions = serviceProvider.GetRequiredService<IOptions<JsonOptions>>();
                    var environment = serviceProvider.GetRequiredService<IWebHostEnvironment>();
                    return new WebAppMiddlewareConfiguration(jsonOptions, environment.IsDevelopment());
                }
            )
            .AddTransient<WebAppMiddleware>();
    }
    
    public static IApplicationBuilder UseWebAppMiddleware(this IApplicationBuilder app)
    {
        if (!File.Exists(WebAppMiddlewareConfiguration.HtmlTemplatePath))
        {
            // When running locally, this code might be called while index.html is recreated, give it a few seconds to finish.
            Thread.Sleep(TimeSpan.FromSeconds(2));
        }
        
        if (!File.Exists(WebAppMiddlewareConfiguration.HtmlTemplatePath))
        {
            throw new InvalidOperationException("The index.html file is missing.");
        }
        
        var webAppConfiguration = app.ApplicationServices.GetRequiredService<WebAppMiddlewareConfiguration>();
        
        return app
            .UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(webAppConfiguration.BuildRootPath) })
            .UseRequestLocalization("en-US", "da-DK")
            .UseMiddleware<WebAppMiddleware>();
    }
}
