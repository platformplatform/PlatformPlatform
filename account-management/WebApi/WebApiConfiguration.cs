using System.Reflection;
using JetBrains.Annotations;
using PlatformPlatform.SharedKernel.AspNetCoreUtils;

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
        services.AddCommonServices();

        return services;
    }
}