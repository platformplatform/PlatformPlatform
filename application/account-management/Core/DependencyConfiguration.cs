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

    public static IHostApplicationBuilder AddAccountManagementInfrastructure(this IHostApplicationBuilder builder)
    {
        // Infrastructure is configured separately from other Infrastructure services to allow mocking in tests
        builder.AddSharedInfrastructure<AccountManagementDbContext>("account-management-database");

        builder.AddNamedBlobStorages(("avatars-storage", "BLOB_STORAGE_URL"));

        return builder;
    }

    public static IServiceCollection AddAccountManagementServices(this IServiceCollection services)
    {
        services.AddSharedServices<AccountManagementDbContext>();
        services.AddProjectServices(Assembly);

        services.AddScoped<IPasswordHasher<object>, PasswordHasher<object>>();
        services.AddScoped<OneTimePasswordHelper>();

        services.AddScoped<AuthenticationTokenGenerator>();
        services.AddScoped<AuthenticationTokenService>();

        services.AddHttpClient("Gravatar").ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler());

        return services;
    }
}
