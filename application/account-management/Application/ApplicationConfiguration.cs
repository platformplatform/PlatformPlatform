using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.SharedKernel.ApplicationCore;

namespace PlatformPlatform.AccountManagement.Application;

public static class ApplicationConfiguration
{
    public static Assembly Assembly => Assembly.GetExecutingAssembly();

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddApplicationCoreServices(Assembly);

        services.AddHttpContextAccessor();

        return services;
    }
}
