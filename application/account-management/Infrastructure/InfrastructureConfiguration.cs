using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlatformPlatform.SharedKernel.InfrastructureCore;

namespace PlatformPlatform.AccountManagement.Infrastructure;

public static class InfrastructureConfiguration
{
    public static Assembly Assembly => Assembly.GetExecutingAssembly();

    public static IServiceCollection AddConfigureStorage(this IServiceCollection services, IHostApplicationBuilder builder)
    {
        // Storage is configured separately from other Infrastructure services to allow mocking in tests
        services.ConfigureDatabaseContext<AccountManagementDbContext>(builder, "account-management-database");
        services.AddDefaultBlobStorage(builder);
        services.AddNamedBlobStorages(builder, ("avatars-storage", "BLOB_STORAGE_URL"));

        return services;
    }

    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.ConfigureInfrastructureCoreServices<AccountManagementDbContext>(Assembly);

        return services;
    }
}
