using System.Reflection;
using FluentValidation;
using JetBrains.Annotations;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.Foundation.DomainModeling.Behaviors;

namespace PlatformPlatform.Foundation.DomainModeling;

public static class DomainModelingConfiguration
{
    [UsedImplicitly]
    public static IServiceCollection AddDomainModelingServices(this IServiceCollection services, Assembly assembly)
    {
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkPipelineBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PublishDomainEventsPipelineBehavior<,>));

        services.AddMediatR(configuration => configuration.RegisterServicesFromAssemblies(assembly));
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}