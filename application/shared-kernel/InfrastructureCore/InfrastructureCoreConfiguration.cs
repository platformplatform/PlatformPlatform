using System.Net.Sockets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlatformPlatform.SharedKernel.ApplicationCore.Services;
using PlatformPlatform.SharedKernel.DomainCore.DomainEvents;
using PlatformPlatform.SharedKernel.DomainCore.Persistence;
using PlatformPlatform.SharedKernel.InfrastructureCore.Persistence;
using PlatformPlatform.SharedKernel.InfrastructureCore.Services;

namespace PlatformPlatform.SharedKernel.InfrastructureCore;

public static class InfrastructureCoreConfiguration
{
    public static readonly bool IsRunningInAzure = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID") is not null;

    public static DefaultAzureCredential DefaultAzureCredential => GetDefaultAzureCredential();

    private static DefaultAzureCredential GetDefaultAzureCredential()
    {
        // Hack: Remove trailing whitespace from the environment variable, added in Bicep to workaround issue #157.
        var managedIdentityClientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID")!.Trim();
        var credentialOptions = new DefaultAzureCredentialOptions { ManagedIdentityClientId = managedIdentityClientId };
        return new DefaultAzureCredential(credentialOptions);
    }

    public static IServiceCollection ConfigureDataProtectionApi(this IServiceCollection services)
    {
        if (IsRunningInAzure)
        {
            var keyIdentifier = $"{Environment.GetEnvironmentVariable("KEYVAULT_URL")}/keys/DataProtectionKey/3186da570c034d9488dcf27fb91b33dc";

            services.AddDataProtection()
                .ProtectKeysWithAzureKeyVault(new Uri(keyIdentifier), DefaultAzureCredential)
                .SetDefaultKeyLifetime(TimeSpan.FromDays(30)); // Rotate keys every 30 days
        }
        else
        {
            var keysPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".aspnet", "DataProtection-Keys");
            services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
                .SetDefaultKeyLifetime(TimeSpan.FromDays(7));
        }

        return services;
    }

    public static IServiceCollection ConfigureDatabaseContext<T>(
        this IServiceCollection services,
        IHostApplicationBuilder builder,
        string connectionName
    ) where T : DbContext
    {
        var connectionString = IsRunningInAzure
            ? Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING")
            : builder.Configuration.GetConnectionString(connectionName);

        builder.Services.AddSqlServer<T>(connectionString, optionsBuilder => { optionsBuilder.UseAzureSqlDefaults(); });
        builder.EnrichSqlServerDbContext<T>();

        return services;
    }

    // Register the default storage account for IBlobStorage
    public static IServiceCollection AddDefaultBlobStorage(this IServiceCollection services, IHostApplicationBuilder builder)
    {
        if (IsRunningInAzure)
        {
            var defaultBlobStorageUri = new Uri(Environment.GetEnvironmentVariable("BLOB_STORAGE_URL")!);
            services.AddSingleton<IBlobStorage>(
                _ => new BlobStorage(new BlobServiceClient(defaultBlobStorageUri, DefaultAzureCredential))
            );
        }
        else
        {
            var connectionString = builder.Configuration.GetConnectionString("blob-storage");
            services.AddSingleton<IBlobStorage>(_ => new BlobStorage(new BlobServiceClient(connectionString)));
        }

        return services;
    }

    // Register different storage accounts for IBlobStorage using .NET Keyed services, when a service needs to access multiple storage accounts
    public static IServiceCollection AddNamedBlobStorages(
        this IServiceCollection services,
        IHostApplicationBuilder builder,
        params (string ConnectionName, string EnvironmentVariable)[] connections
    )
    {
        if (IsRunningInAzure)
        {
            foreach (var connection in connections)
            {
                var storageEndpointUri = new Uri(Environment.GetEnvironmentVariable(connection.EnvironmentVariable)!);
                services.AddKeyedSingleton<IBlobStorage, BlobStorage>(connection.ConnectionName,
                    (_, _) => new BlobStorage(new BlobServiceClient(storageEndpointUri, DefaultAzureCredential))
                );
            }
        }
        else
        {
            var connectionString = builder.Configuration.GetConnectionString("blob-storage");
            services.AddSingleton<IBlobStorage>(_ => new BlobStorage(new BlobServiceClient(connectionString)));
        }

        return services;
    }

    public static IServiceCollection ConfigureInfrastructureCoreServices<T>(
        this IServiceCollection services,
        Assembly assembly
    )
        where T : DbContext
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>(provider => new UnitOfWork(provider.GetRequiredService<T>()));
        services.AddScoped<IDomainEventCollector, DomainEventCollector>(provider =>
            new DomainEventCollector(provider.GetRequiredService<T>())
        );

        services.RegisterRepositories(assembly);

        if (IsRunningInAzure)
        {
            var keyVaultUri = new Uri(Environment.GetEnvironmentVariable("KEYVAULT_URL")!);
            services.AddSingleton(_ => new SecretClient(keyVaultUri, DefaultAzureCredential));

            services.AddTransient<IEmailService, AzureEmailService>();
        }
        else
        {
            services.AddTransient<IEmailService, DevelopmentEmailService>();
        }

        return services;
    }

    private static IServiceCollection RegisterRepositories(this IServiceCollection services, Assembly assembly)
    {
        // Scrutor will scan the assembly for all classes that implement the IRepository
        // and register them as a service in the container.
        services.Scan(scan => scan
            .FromAssemblies(assembly)
            .AddClasses(classes => classes.Where(type =>
                    type.IsClass && (type.IsNotPublic || type.IsPublic)
                                 && type.BaseType is { IsGenericType: true } &&
                                 type.BaseType.GetGenericTypeDefinition() == typeof(RepositoryBase<,>)
                )
            )
            .AsImplementedInterfaces()
            .WithScopedLifetime()
        );

        return services;
    }

    public static void ApplyMigrations<T>(this IServiceProvider services) where T : DbContext
    {
        using var scope = services.CreateScope();

        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger(nameof(InfrastructureCoreConfiguration));

        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
        logger.LogInformation("Applying database migrations. Version: {Version}.", version);

        var retryCount = 1;
        while (retryCount <= 20)
        {
            try
            {
                if (retryCount % 5 == 0) logger.LogInformation("Waiting for databases to be ready...");

                var dbContext = scope.ServiceProvider.GetService<T>() ??
                                throw new UnreachableException("Missing DbContext.");

                var strategy = dbContext.Database.CreateExecutionStrategy();

                strategy.Execute(() => dbContext.Database.Migrate());

                logger.LogInformation("Finished migrating database.");

                break;
            }
            catch (SqlException ex) when (ex.Message.Contains("an error occurred during the pre-login handshake"))
            {
                // Known error in Aspire, when SQL Server is not ready
                retryCount++;
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
            catch (SocketException ex) when (ex.Message.Contains("Invalid argument"))
            {
                // Known error in Aspire, when SQL Server is not ready
                retryCount++;
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while applying migrations.");

                // Wait for the logger to flush
                Thread.Sleep(TimeSpan.FromSeconds(1));

                break;
            }
        }
    }
}
