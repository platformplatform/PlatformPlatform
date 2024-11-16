using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlatformPlatform.BackOffice.Database;
using PlatformPlatform.SharedKernel.Configuration;

namespace PlatformPlatform.BackOffice;

public static class Configuration
{
    public static Assembly Assembly => Assembly.GetExecutingAssembly();

    public static IHostApplicationBuilder AddBackOfficeInfrastructure(this IHostApplicationBuilder builder)
    {
        // Infrastructure is configured separately from other Infrastructure services to allow mocking in tests
        return builder.AddSharedInfrastructure<BackOfficeDbContext>("back-office-database");
    }

    public static IServiceCollection AddBackOfficeServices(this IServiceCollection services)
    {
        return services.AddSharedServices<BackOfficeDbContext>(Assembly);
    }
}
