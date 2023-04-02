using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace PlatformPlatform.Application;

public static class ConfigureServices
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssemblies(ApplicationAssembly.Assembly));

        services.AddValidatorsFromAssembly(ApplicationAssembly.Assembly);

        return services;
    }
}