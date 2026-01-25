using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlatformPlatform.Account.Database;
using PlatformPlatform.Account.Features.EmailAuthentication.Shared;
using PlatformPlatform.Account.Features.ExternalAuthentication;
using PlatformPlatform.Account.Features.ExternalAuthentication.Shared;
using PlatformPlatform.Account.Features.Users.Shared;
using PlatformPlatform.Account.Integrations.Gravatar;
using PlatformPlatform.Account.Integrations.OAuth;
using PlatformPlatform.Account.Integrations.OAuth.Google;
using PlatformPlatform.Account.Integrations.OAuth.Mock;
using PlatformPlatform.SharedKernel.Configuration;
using PlatformPlatform.SharedKernel.OpenIdConnect;

namespace PlatformPlatform.Account;

public static class Configuration
{
    public static Assembly Assembly => Assembly.GetExecutingAssembly();

    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddAccountInfrastructure()
        {
            // Infrastructure is configured separately from other Infrastructure services to allow mocking in tests
            return builder
                .AddSharedInfrastructure<AccountDbContext>("account-database")
                .AddNamedBlobStorages([("account-storage", "BLOB_STORAGE_URL")]);
        }
    }

    extension(IServiceCollection services)
    {
        public IServiceCollection AddAccountServices()
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
                .AddSharedServices<AccountDbContext>([Assembly])
                .AddScoped<StartEmailConfirmation>()
                .AddScoped<CompleteEmailConfirmation>()
                .AddScoped<AvatarUpdater>()
                .AddScoped<UserInfoFactory>()
                .AddScoped<ExternalAuthenticationService>()
                .AddScoped<ExternalAuthenticationHelper>();
        }
    }
}
