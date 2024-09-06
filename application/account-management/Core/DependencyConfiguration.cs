using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlatformPlatform.AccountManagement.Authentication.Services;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.SharedKernel;
using PlatformPlatform.SharedKernel.Authentication;

namespace PlatformPlatform.AccountManagement;

public static class DependencyConfiguration
{
    public static Assembly Assembly => Assembly.GetExecutingAssembly();

    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddInfrastructureCoreServices<AccountManagementDbContext>(Assembly);

        services.AddHttpContextAccessor();

        services.AddScoped<IPasswordHasher<object>, PasswordHasher<object>>();
        services.AddScoped<OneTimePasswordHelper>();

        services.AddScoped<AuthenticationTokenGenerator>();
        services.AddScoped<AuthenticationTokenService>();

        return services;
    }

    public static IServiceCollection AddStorage(this IServiceCollection services, IHostApplicationBuilder builder)
    {
        // Storage is configured separately from other Infrastructure services to allow mocking in tests
        services.ConfigureDatabaseContext<AccountManagementDbContext>(builder, "account-management-database");
        services.AddDefaultBlobStorage(builder);
        services.AddNamedBlobStorages(builder, ("avatars-storage", "BLOB_STORAGE_URL"));

        return services;
    }
}
