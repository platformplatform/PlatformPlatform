using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.Users.Shared;
using PlatformPlatform.AccountManagement.Integrations.Gravatar;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Authentication.TokenGeneration;
using PlatformPlatform.SharedKernel.Configuration;

namespace PlatformPlatform.AccountManagement;

public static class DependencyConfiguration
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
        services.AddHttpClient("Gravatar").ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler());

        return services
            .AddSharedServices<AccountManagementDbContext>(Assembly)
            .AddScoped<IPasswordHasher<object>, PasswordHasher<object>>()
            .AddScoped<OneTimePasswordHelper>()
            .AddScoped<RefreshTokenGenerator>()
            .AddScoped<AccessTokenGenerator>()
            .AddScoped<AuthenticationTokenService>()
            .AddScoped<AvatarUpdater>()
            .AddScoped<GravatarClient>();
    }
}
