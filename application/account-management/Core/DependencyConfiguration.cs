using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.Users.Avatars;
using PlatformPlatform.AccountManagement.Integrations.Gravatar;
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
        services.AddSharedServices<AccountManagementDbContext>(Assembly);

        services.AddScoped<IPasswordHasher<object>, PasswordHasher<object>>();
        services.AddScoped<OneTimePasswordHelper>();

        services.AddScoped<RefreshTokenGenerator>();
        services.AddScoped<AccessTokenGenerator>();
        services.AddScoped<AuthenticationTokenService>();

        services.AddScoped<AvatarUpdater>();
        services.AddScoped<GravatarClient>();
        services.AddHttpClient("Gravatar").ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler());

        return services;
    }
}
