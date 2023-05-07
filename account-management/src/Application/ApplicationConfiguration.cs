using System.Reflection;
using FluentValidation;
using Mapster;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.AccountManagement.Application.Shared.Persistence;
using PlatformPlatform.AccountManagement.Application.Tenants.Dtos;
using PlatformPlatform.AccountManagement.Domain.Tenants;

namespace PlatformPlatform.AccountManagement.Application;

/// <summary>
///     The ApplicationConfiguration class is used to register services used by the application layer
///     with the dependency injection container.
/// </summary>
public static class ApplicationConfiguration
{
    public static readonly Assembly Assembly = Assembly.GetExecutingAssembly();

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssemblies(Assembly));

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkPipelineBehavior<,>));

        services.AddValidatorsFromAssembly(Assembly);

        ConfigureMappings();

        return services;
    }

    /// <summary>
    ///     Configures the mappings between domain entities and DTOs.
    /// </summary>
    public static void ConfigureMappings()
    {
        TypeAdapterConfig<Tenant, TenantDto>.NewConfig()
            .Map(destination => destination.Id, source => source.Id.AsRawString());
    }
}