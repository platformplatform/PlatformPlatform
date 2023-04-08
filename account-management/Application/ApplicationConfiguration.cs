using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace PlatformPlatform.AccountManagement.Application;

public static class ApplicationConfiguration
{
    public static readonly Assembly Assembly = typeof(ApplicationConfiguration).Assembly;

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssemblies(Assembly));

        services.AddValidatorsFromAssembly(Assembly);

        return services;
    }
}