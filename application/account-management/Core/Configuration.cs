using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.EmailAuthentication.Shared;
using PlatformPlatform.AccountManagement.Features.ExternalAuthentication;
using PlatformPlatform.AccountManagement.Features.ExternalAuthentication.Shared;
using PlatformPlatform.AccountManagement.Features.Users.Shared;
using PlatformPlatform.AccountManagement.Integrations.Gravatar;
using PlatformPlatform.AccountManagement.Integrations.OAuth;
using PlatformPlatform.AccountManagement.Integrations.OAuth.Google;
using PlatformPlatform.AccountManagement.Integrations.OAuth.Mock;
using PlatformPlatform.SharedKernel.Configuration;
using PlatformPlatform.SharedKernel.OpenIdConnect;

namespace PlatformPlatform.AccountManagement;

public static class Configuration
{
    public static Assembly Assembly => Assembly.GetExecutingAssembly();

    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddAccountManagementInfrastructure()
        {
            // Infrastructure is configured separately from other Infrastructure services to allow mocking in tests
            return builder
                .AddSharedInfrastructure<AccountManagementDbContext>("account-management-database")
                .AddNamedBlobStorages([("account-management-storage", "BLOB_STORAGE_URL")]);
        }
    }

    extension(IServiceCollection services)
    {
        public IServiceCollection AddAccountManagementServices()
        {
            services.AddHttpClient<GravatarClient>(client =>
                {
                    client.BaseAddress = new Uri("https://gravatar.com/");
                    client.Timeout = TimeSpan.FromSeconds(5);
                }
            );

            services.AddHttpClient<OpenIdConnectConfigurationManagerFactory>(client => { client.Timeout = TimeSpan.FromSeconds(10); });
            services.AddSingleton<OpenIdConnectConfigurationManagerFactory>();

            services.AddHttpClient<ExternalAvatarClient>(client => { client.Timeout = TimeSpan.FromSeconds(10); });

            services.AddHttpClient<GoogleOAuthProvider>(client => { client.Timeout = TimeSpan.FromSeconds(10); });
            services.AddKeyedScoped<IOAuthProvider, GoogleOAuthProvider>("google");
            services.AddKeyedScoped<IOAuthProvider, MockOAuthProvider>("mock-google");
            services.AddScoped<OAuthProviderFactory>();

            return services
                .AddSharedServices<AccountManagementDbContext>([Assembly])
                .AddScoped<StartEmailConfirmation>()
                .AddScoped<CompleteEmailConfirmation>()
                .AddScoped<AvatarUpdater>()
                .AddScoped<UserInfoFactory>()
                .AddScoped<ExternalAuthenticationService>()
                .AddScoped<ExternalAuthenticationHelper>();
        }
    }
}
