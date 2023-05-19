using System.Reflection;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.Foundation.DomainModeling.Persistence;
using PlatformPlatform.Foundation.PersistenceInfrastructure.Persistence;

namespace PlatformPlatform.Foundation.PersistenceInfrastructure;

public static class PersistenceInfrastructureConfiguration
{
    public static IServiceCollection ConfigureDatabaseContext<T>(this IServiceCollection services,
        IConfiguration configuration)
        where T : DbContext
    {
        services.AddDbContext<T>((_, optionsBuilder) =>
        {
            var password = Environment.GetEnvironmentVariable("SQL_DATABASE_PASSWORD")
                           ?? throw new Exception("The 'SQL_DATABASE_PASSWORD' environment variable has not been set.");

            var connectionString = configuration.GetConnectionString("Default");
            connectionString += $";Password={password}";

            optionsBuilder.UseSqlServer(connectionString);
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>(provider => new UnitOfWork(provider.GetRequiredService<T>()));

        return services;
    }

    [UsedImplicitly]
    public static IServiceCollection RegisterRepositories(this IServiceCollection services, Assembly assembly)
    {
        // Scrutor will scan the assembly for all classes that implement the IRepository
        // and register them as a service in the container.
        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses()
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        return services;
    }
}