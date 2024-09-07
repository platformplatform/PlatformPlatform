using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlatformPlatform.BackOffice.Database;
using PlatformPlatform.SharedKernel;

namespace PlatformPlatform.BackOffice;

public static class DependencyConfiguration
{
    public static Assembly Assembly => Assembly.GetExecutingAssembly();

    public static IServiceCollection AddBackOfficeServices(this IServiceCollection services)
    {
        services.AddSharedServices<BackOfficeDbContext>(Assembly);

        return services;
    }

    public static IServiceCollection AddStorage(this IServiceCollection services, IHostApplicationBuilder builder)
    {
        // Storage is configured separately from other Infrastructure services to allow mocking in tests
        services.ConfigureDatabaseContext<BackOfficeDbContext>(builder, "back-office-database");
        services.AddDefaultBlobStorage(builder);

        return services;
    }
}
