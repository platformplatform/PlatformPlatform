using Account.Database;
using Account.Features.EmailAuthentication.Shared;
using Account.Features.ExternalAuthentication;
using Account.Features.ExternalAuthentication.Shared;
using Account.Features.FeatureFlags;
using Account.Features.Subscriptions.Shared;
using Account.Features.Users.Shared;
using Account.Integrations.Gravatar;
using Account.Integrations.OAuth;
using Account.Integrations.OAuth.Google;
using Account.Integrations.OAuth.Mock;
using Account.Integrations.Stripe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharedKernel.Configuration;
using SharedKernel.OpenIdConnect;

namespace Account;

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

            services.AddMemoryCache();
            services.AddKeyedScoped<IStripeClient, StripeClient>("stripe");
            services.AddKeyedScoped<IStripeClient, MockStripeClient>("mock-stripe");
            services.AddKeyedScoped<IStripeClient, UnconfiguredStripeClient>("unconfigured-stripe");
            services.AddScoped<StripeClientFactory>();

            return services
                .AddSharedServices<AccountDbContext>([Assembly])
                .AddScoped<StartEmailConfirmation>()
                .AddScoped<CompleteEmailConfirmation>()
                .AddScoped<AvatarUpdater>()
                .AddScoped<FeatureFlagEvaluationService>()
                .AddScoped<UserInfoFactory>()
                .AddScoped<ProcessPendingStripeEvents>()
                .AddScoped<ExternalAuthenticationService>()
                .AddScoped<ExternalAuthenticationHelper>();
        }
    }
}
