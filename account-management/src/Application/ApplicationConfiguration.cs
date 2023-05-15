using System.Reflection;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.AccountManagement.Application.Tenants.Dtos;
using PlatformPlatform.Foundation.Application.DomainEvents;
using PlatformPlatform.Foundation.Application.Persistence;

namespace PlatformPlatform.AccountManagement.Application;

/// <summary>
///     The ApplicationConfiguration class is used to register services used by the application layer
///     with the dependency injection container.
/// </summary>
public static class ApplicationConfiguration
{
    public static Assembly Assembly => Assembly.GetExecutingAssembly();

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssemblies(Assembly));

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkPipelineBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PublishDomainEventsPipelineBehavior<,>));

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
        TenantDto.ConfigureTenantDtoMapping();
    }
}