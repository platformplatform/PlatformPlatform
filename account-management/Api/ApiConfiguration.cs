using PlatformPlatform.SharedKernel.ApiCore;

namespace PlatformPlatform.AccountManagement.Api;

public static class WebApiConfiguration
{
    public static readonly Assembly Assembly = Assembly.GetExecutingAssembly();

    [UsedImplicitly]
    public static IServiceCollection AddApiServices(this IServiceCollection services, WebApplicationBuilder builder)
    {
        services.AddCommonServices(builder);

        return services;
    }
}