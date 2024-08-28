using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlatformPlatform.BackOffice.Core.Database;
using PlatformPlatform.SharedKernel;

namespace PlatformPlatform.BackOffice.Core;

public static class DependencyConfiguration
{
    public static Assembly Assembly => Assembly.GetExecutingAssembly();

    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddApplicationCoreServices(Assembly);
        services.AddInfrastructureCoreServices<BackOfficeDbContext>(Assembly);

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
