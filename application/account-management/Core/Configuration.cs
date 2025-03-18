using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.Users.Shared;
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

    public static IServiceCollection AddAccountManagementServices(this IServiceCollection services)
    {
        services.AddHttpClient<GravatarClient>(client =>
            {
                client.BaseAddress = new Uri("https://gravatar.com/");
                client.Timeout = TimeSpan.FromSeconds(5);
            }
        );

        return services
            .AddSharedServices<AccountManagementDbContext>(Assembly)
            .AddScoped<AvatarUpdater>();
    }
}
