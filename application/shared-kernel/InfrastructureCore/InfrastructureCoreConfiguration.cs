using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.SharedKernel.DomainCore.DomainEvents;
using PlatformPlatform.SharedKernel.DomainCore.Persistence;
using PlatformPlatform.SharedKernel.InfrastructureCore.Persistence;

namespace PlatformPlatform.SharedKernel.InfrastructureCore;

public static class InfrastructureCoreConfiguration
{
    private static string? _cachedConnectionString;

    [UsedImplicitly]
    public static IServiceCollection ConfigureInfrastructureCoreServices<T>(this IServiceCollection services,
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
        if (Environment.GetEnvironmentVariable("SWAGGER_GENERATOR") == "true") return services;

        services.AddDbContext<T>((_, options) => options.UseSqlServer(GetConnectionString(configuration)));

        services.AddScoped<IUnitOfWork, UnitOfWork>(provider => new UnitOfWork(provider.GetRequiredService<T>()));
        services.AddScoped<IDomainEventCollector, DomainEventCollector>(provider =>
            new DomainEventCollector(provider.GetRequiredService<T>()));

        return services;
    }

    private static string GetConnectionString(IConfiguration configuration)
    {
        if (_cachedConnectionString is not null) return _cachedConnectionString;

        var serverName = Environment.GetEnvironmentVariable("AZURE_SQL_SERVER_NAME");
        var databaseName = Environment.GetEnvironmentVariable("AZURE_SQL_DATABASE_NAME");
        var managedIdentityId = Environment.GetEnvironmentVariable("MANAGED_IDENTITY_CLIENT_ID");

        var isRunningInAzure = serverName is not null && databaseName is not null && managedIdentityId is not null;
        if (isRunningInAzure)
        {
            _cachedConnectionString = $"""
                                       Server=tcp:{serverName}.database.windows.net,1433;
                                       Initial Catalog={databaseName};
                                       User Id={managedIdentityId};
                                       Authentication=Active Directory Default;TrustServerCertificate=True;
                                       """;
        }
        else
        {
            var connectionString = configuration.GetConnectionString("Default")
                                   ?? throw new InvalidOperationException("Missing ConnectionString configuration.");

            var password = Environment.GetEnvironmentVariable("SQL_SERVER_PASSWORD")
                           ?? throw new InvalidOperationException("Missing SQL_SERVER_PASSWORD environment variable.");

            // When running in Docker (on localhost) the SQL Sever name is configured in the docker-compose.yml
            var sqlServerName = Environment.GetEnvironmentVariable("SQL_SERVER_NAME") ?? "localhost";

            _cachedConnectionString = connectionString
                .Replace("${SQL_SERVER_NAME}", sqlServerName)
                .Replace("${SQL_SERVER_PASSWORD}", password);
        }

        return _cachedConnectionString;
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
        if (Environment.GetEnvironmentVariable("SWAGGER_GENERATOR") == "true") return;

        using var scope = services.CreateScope();

        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(nameof(InfrastructureCoreConfiguration));
        try
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";

            logger.LogInformation("Start migrating database. Version: {Version}", version);

            var dbContext = scope.ServiceProvider.GetService<T>() ??
                            throw new UnreachableException("Missing DbContext.");
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