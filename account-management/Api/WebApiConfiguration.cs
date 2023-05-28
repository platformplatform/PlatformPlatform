using System.Reflection;
using JetBrains.Annotations;
using PlatformPlatform.SharedKernel.ApiCore;

namespace PlatformPlatform.AccountManagement.Api;

public static class WebApiConfiguration
{
    public static readonly Assembly Assembly = Assembly.GetExecutingAssembly();

    [UsedImplicitly]
    public static IServiceCollection AddWebApiServices(this IServiceCollection services)
    {
        services.AddCommonServices();

        return services;
    }
}