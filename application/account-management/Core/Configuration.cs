using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.Users.Shared;
using PlatformPlatform.AccountManagement.Integrations.Gravatar;
using PlatformPlatform.AccountManagement.Integrations.OAuth;
using PlatformPlatform.AccountManagement.Integrations.OAuth.Google;
using PlatformPlatform.AccountManagement.Integrations.OAuth.Mock;
using PlatformPlatform.AccountManagement.Integrations.Stripe;
using PlatformPlatform.SharedKernel.Configuration;

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

            services.AddHttpClient<GoogleOAuthProvider>(client => { client.Timeout = TimeSpan.FromSeconds(10); });
            services.AddKeyedScoped<IOAuthProvider, GoogleOAuthProvider>("google");
            services.AddKeyedScoped<IOAuthProvider, MockOAuthProvider>("mock-google");
            services.AddScoped<OAuthProviderFactory>();

            services.AddKeyedScoped<IStripeClient, StripeClient>("stripe");
            services.AddKeyedScoped<IStripeClient, MockStripeClient>("mock-stripe");
            services.AddKeyedScoped<IStripeClient, UnconfiguredStripeClient>("unconfigured-stripe");
            services.AddScoped<StripeClientFactory>();

            return services
                .AddSharedServices<AccountManagementDbContext>([Assembly])
                .AddScoped<AvatarUpdater>()
                .AddScoped<UserInfoFactory>();
        }
    }
}
