using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.SharedKernel.DomainCore.DomainEvents;
using PlatformPlatform.SharedKernel.DomainCore.Persistence;
using PlatformPlatform.SharedKernel.InfrastructureCore.Persistence;

namespace PlatformPlatform.SharedKernel.InfrastructureCore;

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
        services.AddDbContext<T>((_, options) => options.UseSqlServer(GetConnectionString(configuration)));

        services.AddScoped<IUnitOfWork, UnitOfWork>(provider => new UnitOfWork(provider.GetRequiredService<T>()));
        services.AddScoped<IDomainEventCollector, DomainEventCollector>(provider =>
            new DomainEventCollector(provider.GetRequiredService<T>()));

        return services;
    }

    private static string GetConnectionString(IConfiguration configuration)
    {
        if (Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") is not null)
        {
            // App is running in Azure
            var serverName = Environment.GetEnvironmentVariable("SQL_SERVER_NAME")
                             ?? throw new Exception("Missing SQL_SERVER_NAME environment variable.");

            var databaseName = Environment.GetEnvironmentVariable("SQL_DATABASE_NAME")
                               ?? throw new Exception("Missing SQL_DATABASE_NAME environment variable.");

            return
                $"Server=tcp:{serverName}.database.windows.net,1433;Database={databaseName};Authentication=Active Directory Managed Identity;TrustServerCertificate=False;Encrypt=True;Connection Timeout=30;";
        }

        // App is running locally
        var connectionString = configuration.GetConnectionString("Default")
                               ?? throw new Exception("Missing GetConnectionString configuration.");

        if (Environment.GetEnvironmentVariable("SQL_DATABASE_PASSWORD") is { } password)
        {
            connectionString += $";Password={password}";
        }

        return connectionString;
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

    public static void ApplyMigrations<T>(this IServiceProvider services) where T : DbContext
    {
        using var scope = services.CreateScope();

        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(nameof(InfrastructureCoreConfiguration));
        try
        {
            logger.LogInformation("Start migrating database");

            var dbContext = scope.ServiceProvider.GetService<T>() ?? throw new Exception("Missing DbContext.");
            dbContext.Database.Migrate();

            logger.LogInformation("Finished migrating database");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while applying migrations");

            // Wait for the logger to flush
            Thread.Sleep(TimeSpan.FromSeconds(1));
        }
    }
}