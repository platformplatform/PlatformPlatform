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
    public static IServiceCollection AddDomainModelingServices(this IServiceCollection services,
        Assembly applicationAssembly, Assembly domainAssembly)
    {
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkPipelineBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PublishDomainEventsPipelineBehavior<,>));

        services.AddMediatR(configuration => configuration.RegisterServicesFromAssemblies(applicationAssembly));
        services.AddValidatorsFromAssembly(applicationAssembly);
        services.AddDomainValidatorsFromAssembly(domainAssembly);

        return services;
    }

    private static IServiceCollection AddDomainValidatorsFromAssembly(this IServiceCollection services,
        Assembly assembly)
    {
        var validatorTypes = assembly.GetTypes()
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>)))
            .ToArray();

        foreach (var validatorType in validatorTypes)
        {
            var validatorInterface = validatorType.GetInterfaces()
                .First(i => i.GetGenericTypeDefinition() == typeof(IValidator<>));
            services.AddTransient(validatorInterface, validatorType);
        }

        return services;
    }
}