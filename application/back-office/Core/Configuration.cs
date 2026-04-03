using BackOffice.Database;
using BackOffice.Features.FeatureFlags;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SharedKernel.Configuration;

namespace BackOffice;

public static class Configuration
{
    public static Assembly Assembly => Assembly.GetExecutingAssembly();

    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddBackOfficeInfrastructure()
        {
            // Infrastructure is configured separately from other Infrastructure services to allow mocking in tests
            return builder.AddSharedInfrastructure<BackOfficeDbContext>("back-office-database");
        }
    }

    extension(IServiceCollection services)
    {
        public IServiceCollection AddBackOfficeServices()
        {
            services.AddHttpClient<AccountApiClient>(accountApiHttpClient => { accountApiHttpClient.BaseAddress = new Uri(Environment.GetEnvironmentVariable("ACCOUNT_API_URL") ?? "https://localhost:9100"); }
            );

            return services.AddSharedServices<BackOfficeDbContext>([Assembly]);
        }
    }
}
