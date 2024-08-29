using System.Net.Sockets;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using FluentValidation;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Behaviors;
using PlatformPlatform.SharedKernel.DomainEvents;
using PlatformPlatform.SharedKernel.Persistence;
using PlatformPlatform.SharedKernel.Services;
using PlatformPlatform.SharedKernel.TelemetryEvents;

namespace PlatformPlatform.SharedKernel;

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

    public static IServiceCollection AddMediatRPipelineBehaviours(this IServiceCollection services, Assembly applicationAssembly)
    {
        // Order is important! First all Pre behaviors run, then the command is handled, then all Post behaviors run.
        // So Validation -> Command -> PublishDomainEvents -> UnitOfWork -> PublishTelemetryEvents.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehavior<,>)); // Pre
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PublishTelemetryEventsPipelineBehavior<,>)); // Post
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkPipelineBehavior<,>)); // Post
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PublishDomainEventsPipelineBehavior<,>)); // Post
        services.AddScoped<ITelemetryEventsCollector, TelemetryEventsCollector>();
        services.AddScoped<ConcurrentCommandCounter>();

        services.AddMediatR(configuration => configuration.RegisterServicesFromAssemblies(applicationAssembly));
        services.AddValidatorsFromAssembly(applicationAssembly);

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

    public static IServiceCollection AddInfrastructureCoreServices<T>(this IServiceCollection services, Assembly assembly)
        where T : DbContext
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>(provider => new UnitOfWork(provider.GetRequiredService<T>()));
        services.AddScoped<IDomainEventCollector, DomainEventCollector>(provider =>
            new DomainEventCollector(provider.GetRequiredService<T>())
        );

        var tokenSigningService = GetTokenSigningService();
        services.AddSingleton(tokenSigningService);

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

    public static ITokenSigningService GetTokenSigningService()
    {
        if (IsRunningInAzure)
        {
            var keyVaultUri = new Uri(Environment.GetEnvironmentVariable("KEYVAULT_URL")!);
            var keyClient = new KeyClient(keyVaultUri, DefaultAzureCredential);
            var cryptographyClient = new CryptographyClient(keyClient.GetKey("authentication-token-signing-key").Value.Id, DefaultAzureCredential);

            var secretClient = new SecretClient(keyVaultUri, DefaultAzureCredential);
            var issuer = secretClient.GetSecret("authentication-token-issuer").Value.Value;
            var audience = secretClient.GetSecret("authentication-token-audience").Value.Value;

            return new AzureTokenSigningService(cryptographyClient, issuer, audience);
        }

        return new DevelopmentTokenSigningService();
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
