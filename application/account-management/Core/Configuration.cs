using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.Users.Shared;
using PlatformPlatform.AccountManagement.Integrations.Geoapify;
using PlatformPlatform.AccountManagement.Integrations.Gravatar;
using PlatformPlatform.SharedKernel.Configuration;

namespace PlatformPlatform.AccountManagement;

public static class Configuration
{
    public static Assembly Assembly => Assembly.GetExecutingAssembly();

    public static IHostApplicationBuilder AddAccountManagementInfrastructure(this IHostApplicationBuilder builder)
    {
        // Infrastructure is configured separately from other Infrastructure services to allow mocking in tests
        return builder
            .AddSharedInfrastructure<AccountManagementDbContext>("account-management-database")
            .AddNamedBlobStorages(("avatars-storage", "BLOB_STORAGE_URL"));
    }

    public static IServiceCollection AddAccountManagementServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddMemoryCache(); // Add memory cache for caching functionality

        services.AddHttpClient<GravatarClient>(client =>
            {
                client.BaseAddress = new Uri("https://gravatar.com/");
                client.Timeout = TimeSpan.FromSeconds(5);
            }
        );

        services.AddGeoapifyClient();

        return services
            .AddSharedServices<AccountManagementDbContext>(Assembly)
            .AddScoped<AvatarUpdater>();
    }

    private static IServiceCollection AddGeoapifyClient(this IServiceCollection services)
    {
        services.AddHttpClient<GeoapifyClient>(client =>
            {
                client.BaseAddress = new Uri("https://api.geoapify.com/v1/geocode/");
                client.Timeout = TimeSpan.FromSeconds(10);
            }
        );

        // Register the cached decorator that wraps the concrete GeoapifyClient
        services.AddScoped<IGeoapifyClient>(provider =>
            {
                var concreteClient = provider.GetRequiredService<GeoapifyClient>();
                var memoryCache = provider.GetRequiredService<IMemoryCache>();
                var logger = provider.GetRequiredService<ILogger<CachedGeoapifyClient>>();
                return new CachedGeoapifyClient(concreteClient, memoryCache, logger);
            }
        );

        return services;
    }
}
