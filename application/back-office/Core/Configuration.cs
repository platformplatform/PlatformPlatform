using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlatformPlatform.BackOffice.Database;
using PlatformPlatform.SharedKernel.Configuration;

namespace PlatformPlatform.BackOffice;

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
            return services.AddSharedServices<BackOfficeDbContext>(Assembly);
        }
    }
}
