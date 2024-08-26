using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.AccountManagement.Application.Authentication;
using PlatformPlatform.SharedKernel.ApplicationCore;

namespace PlatformPlatform.AccountManagement.Application;

public static class ApplicationConfiguration
{
    public static Assembly Assembly => Assembly.GetExecutingAssembly();

    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplicationCoreServices(Assembly);

        services.AddHttpContextAccessor();

        services.AddSingleton<AuthenticationTokenGenerator>();
        services.AddTransient<AuthenticationTokenService>();

        return services;
    }
}
