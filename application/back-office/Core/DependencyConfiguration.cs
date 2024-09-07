using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlatformPlatform.BackOffice.Database;
using PlatformPlatform.SharedKernel;

namespace PlatformPlatform.BackOffice;

public static class DependencyConfiguration
{
    public static Assembly Assembly => Assembly.GetExecutingAssembly();

    public static IHostApplicationBuilder AddBackOfficeInfrastructure(this IHostApplicationBuilder builder)
    {
        // Storage is configured separately from other Infrastructure services to allow mocking in tests
        builder.ConfigureDatabaseContext<BackOfficeDbContext>("back-office-database");
        builder.AddDefaultBlobStorage();

        return builder;
    }

    public static IServiceCollection AddBackOfficeServices(this IServiceCollection services)
    {
        services.AddSharedServices<BackOfficeDbContext>(Assembly);

        return services;
    }
}
