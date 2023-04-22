using System.Reflection;
using Microsoft.OpenApi.Models;

namespace PlatformPlatform.AccountManagement.WebApi;

/// <summary>
///     The WebApiConfiguration class is used to register services used by the Web API
///     with the dependency injection container.
/// </summary>
public static class WebApiConfiguration
{
    public static readonly Assembly Assembly = Assembly.GetExecutingAssembly();

    public static IServiceCollection AddWebApiServices(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo {Title = "Platform Platform API", Version = "v1"});
        });

        return services;
    }
}