using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlatformPlatform.SharedKernel.InfrastructureCore;

namespace PlatformPlatform.BackOffice.Infrastructure;

public static class InfrastructureConfiguration
{
    public static Assembly Assembly => Assembly.GetExecutingAssembly();

    public static IServiceCollection AddConfigureStorage(this IServiceCollection services, IHostApplicationBuilder builder)
    {
        // Storage is configured separately from other Infrastructure services to allow mocking in tests
        services.ConfigureDatabaseContext<BackOfficeDbContext>(builder, "back-office-database");
        services.AddDefaultBlobStorage(builder);

        return services;
    }

    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.ConfigureInfrastructureCoreServices<BackOfficeDbContext>(Assembly);

        return services;
    }
}
