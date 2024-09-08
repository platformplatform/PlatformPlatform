using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlatformPlatform.SharedKernel.Services;

namespace PlatformPlatform.SharedKernel;

public static class SharedInfrastructureConfiguration
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
}
