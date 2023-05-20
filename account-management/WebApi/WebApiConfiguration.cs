using System.Reflection;
using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.WebApi.Tenants;
using PlatformPlatform.Foundation.AspNetCoreUtils;

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

        ConfigureMappings();

        return services;
    }

    /// <summary>
    ///     Configures the mappings between domain entities and DTOs. This is done using Mapster, which uses
    ///     convention-based mapping, which means no configuration is needed if properties are named the same in both
    ///     the DTO and the Entity. However, it can be configured to use explicit mappings.
    /// </summary>
    private static void ConfigureMappings()
    {
        TenantResponseDto.ConfigureTenantDtoMapping();
    }
}