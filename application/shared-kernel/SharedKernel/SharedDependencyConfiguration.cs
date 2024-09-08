using System.Text.Json;
using Azure.Identity;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Keys.Cryptography;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using FluentValidation;
using Microsoft.AspNetCore.Http.Json;
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

public static class SharedDependencyConfiguration
{
    public static readonly bool IsRunningInAzure = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID") is not null;

    // Ensure that enums are serialized as strings and use CamelCase
    public static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static DefaultAzureCredential DefaultAzureCredential => GetDefaultAzureCredential();

    private static DefaultAzureCredential GetDefaultAzureCredential()
    {
        // Hack: Remove trailing whitespace from the environment variable, added in Bicep to workaround issue #157.
        var managedIdentityClientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID")!.Trim();
        var credentialOptions = new DefaultAzureCredentialOptions { ManagedIdentityClientId = managedIdentityClientId };
        return new DefaultAzureCredential(credentialOptions);
    }

    public static IHostApplicationBuilder ConfigureDatabaseContext<T>(this IHostApplicationBuilder builder, string connectionName)
        where T : DbContext
    {
        var connectionString = IsRunningInAzure
            ? Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING")
            : builder.Configuration.GetConnectionString(connectionName);

        builder.Services.AddSqlServer<T>(connectionString, optionsBuilder => { optionsBuilder.UseAzureSqlDefaults(); });
        builder.EnrichSqlServerDbContext<T>();

        return builder;
    }

    // Register the default storage account for IBlobStorage
    public static IHostApplicationBuilder AddDefaultBlobStorage(this IHostApplicationBuilder builder)
    {
        if (IsRunningInAzure)
        {
            var defaultBlobStorageUri = new Uri(Environment.GetEnvironmentVariable("BLOB_STORAGE_URL")!);
            builder.Services.AddSingleton<IBlobStorage>(
                _ => new BlobStorage(new BlobServiceClient(defaultBlobStorageUri, DefaultAzureCredential))
            );
        }
        else
        {
            var connectionString = builder.Configuration.GetConnectionString("blob-storage");
            builder.Services.AddSingleton<IBlobStorage>(_ => new BlobStorage(new BlobServiceClient(connectionString)));
        }

        return builder;
    }

    // Register different storage accounts for IBlobStorage using .NET Keyed services, when a service needs to access multiple storage accounts
    public static IHostApplicationBuilder AddNamedBlobStorages(
        this IHostApplicationBuilder builder,
        params (string ConnectionName, string EnvironmentVariable)[] connections
    )
    {
        if (IsRunningInAzure)
        {
            foreach (var connection in connections)
            {
                var storageEndpointUri = new Uri(Environment.GetEnvironmentVariable(connection.EnvironmentVariable)!);
                builder.Services.AddKeyedSingleton<IBlobStorage, BlobStorage>(connection.ConnectionName,
                    (_, _) => new BlobStorage(new BlobServiceClient(storageEndpointUri, DefaultAzureCredential))
                );
            }
        }
        else
        {
            var connectionString = builder.Configuration.GetConnectionString("blob-storage");
            builder.Services.AddSingleton<IBlobStorage>(_ => new BlobStorage(new BlobServiceClient(connectionString)));
        }

        return builder;
    }

    public static IServiceCollection AddSharedServices<T>(this IServiceCollection services, Assembly assembly)
        where T : DbContext
    {
        // Even though the HttpContextAccessor is not available in Worker Services, it is still registered here because
        // worker services register the same CommandHandlers as the API, which may require the HttpContext.
        // Consider making a generic IRequestContextProvider that can return the HttpContext only if it is available.
        services.AddHttpContextAccessor();

        services.Configure<JsonOptions>(options =>
            {
                // Copy the default options from the DefaultJsonSerializerOptions to enforce consistency in serialization.
                foreach (var jsonConverter in DefaultJsonSerializerOptions.Converters)
                {
                    options.SerializerOptions.Converters.Add(jsonConverter);
                }

                options.SerializerOptions.PropertyNamingPolicy = DefaultJsonSerializerOptions.PropertyNamingPolicy;
            }
        );

        services.AddMediatRPipelineBehaviours(assembly);

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

        services.AddSingleton(GetTokenSigningService());

        return services;
    }

    private static IServiceCollection AddMediatRPipelineBehaviours(this IServiceCollection services, Assembly assembly)
    {
        // Order is important! First all Pre-behaviors run, then the command is handled, and finally all Post behaviors run.
        // So Validation → Command → PublishDomainEvents → UnitOfWork → PublishTelemetryEvents.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehavior<,>)); // Pre
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PublishTelemetryEventsPipelineBehavior<,>)); // Post
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkPipelineBehavior<,>)); // Post
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PublishDomainEventsPipelineBehavior<,>)); // Post
        services.AddScoped<ITelemetryEventsCollector, TelemetryEventsCollector>();
        services.AddScoped<ConcurrentCommandCounter>();

        services.AddMediatR(configuration => configuration.RegisterServicesFromAssemblies(assembly));
        services.AddValidatorsFromAssembly(assembly);

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
}
