using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace PlatformPlatform.Application;

public static class ConfigureServices
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var applicationAssembly = typeof(ConfigureServices).Assembly;

        services.AddMediatR(configuration => configuration.RegisterServicesFromAssemblies(applicationAssembly));
        services.AddValidatorsFromAssembly(applicationAssembly);

        return services;
    }
}