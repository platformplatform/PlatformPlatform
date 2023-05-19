using System.Reflection;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.Foundation.Application.DomainEvents;
using PlatformPlatform.Foundation.Application.Persistence;
using PlatformPlatform.Foundation.DddCore;
using PlatformPlatform.Foundation.Infrastructure;

namespace PlatformPlatform.Foundation;

public static class DependencyInjection
{
    public static IServiceCollection AddFoundationServices(this IServiceCollection services)
    {
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkPipelineBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PublishDomainEventsPipelineBehavior<,>));

        return services;
    }

    public static void ConfigureDatabaseContext<T>(this IServiceCollection services, IConfiguration configuration)
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
    }

    public static void RegisterRepositories(this IServiceCollection services, Assembly assembly)
    {
        // Scrutor will scan the assembly for all classes that implement the IRepository
        // and register them as a service in the container.
        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses()
            .AsImplementedInterfaces()
            .WithScopedLifetime());
    }
}