using System.Reflection;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.Foundation.DomainModeling.Persistence;
using PlatformPlatform.Foundation.InfrastructureCore.EntityFramework;
using PlatformPlatform.Foundation.InfrastructureCore.Persistence;

namespace PlatformPlatform.Foundation.InfrastructureCore;

public static class InfrastructureCoreConfiguration
{
    [UsedImplicitly]
    public static IServiceCollection ConfigurePersistence<T>(this IServiceCollection services,
        IConfiguration configuration, Assembly assembly) where T : DbContext
    {
        services.ConfigureDatabaseContext<T>(configuration);

        services.RegisterRepositories(assembly);

        return services;
    }

    [UsedImplicitly]
    private static IServiceCollection ConfigureDatabaseContext<T>(this IServiceCollection services,
        IConfiguration configuration)
        where T : DbContext
    {
        services.AddScoped<EntityValidationSaveChangesInterceptor>();

        services.AddDbContext<T>((provider, optionsBuilder) =>
        {
            var password = Environment.GetEnvironmentVariable("SQL_DATABASE_PASSWORD")
                           ?? throw new Exception("The 'SQL_DATABASE_PASSWORD' environment variable has not been set.");

            var connectionString = configuration.GetConnectionString("Default");
            connectionString += $";Password={password}";

            optionsBuilder.UseSqlServer(connectionString)
                .AddInterceptors(provider.GetRequiredService<EntityValidationSaveChangesInterceptor>());
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>(provider => new UnitOfWork(provider.GetRequiredService<T>()));

        return services;
    }

    [UsedImplicitly]
    private static IServiceCollection RegisterRepositories(this IServiceCollection services, Assembly assembly)
    {
        // Scrutor will scan the assembly for all classes that implement the IRepository
        // and register them as a service in the container.
        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes.Where(type => type.IsClass && (type.IsNotPublic || type.IsPublic)))
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        return services;
    }
}