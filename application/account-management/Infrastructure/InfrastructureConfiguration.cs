using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlatformPlatform.SharedKernel.InfrastructureCore;

namespace PlatformPlatform.AccountManagement.Infrastructure;

public static class InfrastructureConfiguration
{
    public static Assembly Assembly => Assembly.GetExecutingAssembly();

    public static IServiceCollection AddDatabaseContext(
        this IServiceCollection services,
        IHostApplicationBuilder builder
    )
    {
        services.ConfigureDatabaseContext<AccountManagementDbContext>(builder, "account-management");

        return services;
    }

    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        services.ConfigureInfrastructureCoreServices<AccountManagementDbContext>(Assembly);

        return services;
    }
}