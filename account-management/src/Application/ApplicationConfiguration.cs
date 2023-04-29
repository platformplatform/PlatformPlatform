using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.AccountManagement.Application.Shared.Behaviors;

namespace PlatformPlatform.AccountManagement.Application;

/// <summary>
///     The ApplicationConfiguration class is used to register services used by the application layer
///     with the dependency injection container.
/// </summary>
public static class ApplicationConfiguration
{
    public static readonly Assembly Assembly = Assembly.GetExecutingAssembly();

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssemblies(Assembly));

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehavior<,>));

        services.AddValidatorsFromAssembly(Assembly);

        return services;
    }
}