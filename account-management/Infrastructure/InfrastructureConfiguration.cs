using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.SharedKernel.InfrastructureCore;

namespace PlatformPlatform.AccountManagement.Infrastructure;

/// <summary>
///     The InfrastructureConfiguration class is used to register services used by the infrastructure layer
///     with the dependency injection container.
/// </summary>
public static class InfrastructureConfiguration
{
    public static Assembly Assembly => Assembly.GetExecutingAssembly();

    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.ConfigurePersistence<ApplicationDbContext>(configuration, Assembly);

        return services;
    }
}