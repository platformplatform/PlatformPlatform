using System.Reflection;
using JetBrains.Annotations;
using Microsoft.OpenApi.Models;
using PlatformPlatform.Foundation.AspNetCoreUtils;
using PlatformPlatform.Foundation.AspNetCoreUtils.Middleware;

namespace PlatformPlatform.AccountManagement.WebApi;

/// <summary>
///     The WebApiConfiguration class is used to register services used by the Web API
///     with the dependency injection container.
/// </summary>
public static class WebApiConfiguration
{
    public static readonly Assembly Assembly = Assembly.GetExecutingAssembly();

    [UsedImplicitly]
    public static IServiceCollection AddWebApiServices(this IServiceCollection services)
    {
        services.AddTransient<GlobalExceptionHandlerMiddleware>();

        services.AddEndpointsApiExplorer();

        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo {Title = "PlatformPlatform API", Version = "v1"});
        });

        return services;
    }
}