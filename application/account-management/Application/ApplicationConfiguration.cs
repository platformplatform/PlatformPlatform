using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.AccountManagement.Application.Authentication;
using PlatformPlatform.SharedKernel.ApplicationCore;
using PlatformPlatform.SharedKernel.ApplicationCore.Authentication;

namespace PlatformPlatform.AccountManagement.Application;

public static class ApplicationConfiguration
{
    public static Assembly Assembly => Assembly.GetExecutingAssembly();

    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplicationCoreServices(Assembly);

        services.AddHttpContextAccessor();

        var securityTokenSettings = configuration.GetSection("SecurityTokenSettings").Get<SecurityTokenSettings>()
                                    ?? throw new InvalidOperationException("No SecurityTokenSettings configuration found.");
        services.AddSingleton(securityTokenSettings);

        services.AddSingleton<SecurityTokenGenerator>();
        services.AddTransient<SecurityTokenService>();

        return services;
    }
}
