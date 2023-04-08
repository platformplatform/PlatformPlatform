using System.Reflection;
using Microsoft.OpenApi.Models;

namespace PlatformPlatform.AccountManagement.WebApi;

public static class WebApiConfiguration
{
    public static readonly Assembly Assembly = typeof(WebApiConfiguration).Assembly;

    public static IServiceCollection AddWebApiServices(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        // Register Swagger services
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo {Title = "Platform Platform API", Version = "v1"});
        });
        return services;
    }
}