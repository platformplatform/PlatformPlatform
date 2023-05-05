using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PlatformPlatform.AccountManagement.Infrastructure;

/// <summary>
///     The InfrastructureConfiguration class is used to register services used by the infrastructure layer
///     with the dependency injection container.
/// </summary>
public static class InfrastructureConfiguration
{
    public static readonly Assembly Assembly = Assembly.GetExecutingAssembly();

    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>((_, optionsBuilder) =>
        {
            var password = Environment.GetEnvironmentVariable("SQL_DATABASE_PASSWORD")
                           ?? throw new Exception(
                               "The 'SQL_DATABASE_PASSWORD' environment variable has not been set. Please refer to the instructions in the 'README.md' file to set it up.");

            var connectionString = configuration.GetConnectionString("Default");
            connectionString += $";Password={password}";

            optionsBuilder.UseSqlServer(connectionString);
        });

        // Scrutor will scan the assembly for all classes that implement the IRepository
        // and register them as a service in the container.
        services.Scan(scan => scan
            .FromAssemblies(Assembly)
            .AddClasses()
            .AsImplementedInterfaces()
            .WithScopedLifetime());

        return services;
    }
}