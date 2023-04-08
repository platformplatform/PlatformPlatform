using System.Reflection;

namespace PlatformPlatform.AccountManagement.WebApi;

public static class WebApiConfiguration
{
    public static readonly Assembly Assembly = typeof(WebApiConfiguration).Assembly;

    public static IServiceCollection AddWebApiServices(this IServiceCollection services)
    {
        return services;
    }
}