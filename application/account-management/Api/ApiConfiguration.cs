using Microsoft.AspNetCore.Identity;
using PlatformPlatform.SharedKernel.Application;
using PlatformPlatform.SharedKernel.Infrastructure;

namespace PlatformPlatform.AccountManagement.Api;

public static class ApiConfiguration
{
    public static Assembly Assembly => Assembly.GetExecutingAssembly();

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IPasswordHasher<object>, PasswordHasher<object>>();

        services.AddApplicationServices(Assembly);

        return services;
    }

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
        services.ConfigureInfrastructureServices<AccountManagementDbContext>(Assembly);

        return services;
    }
}
