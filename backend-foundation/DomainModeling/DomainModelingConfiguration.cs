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
        services.AddNonGenericValidators(applicationAssembly);

        return services;
    }

    private static void AddNonGenericValidators(this IServiceCollection services, Assembly assembly)
    {
        var validators = assembly.GetTypes()
            .Where(type => type is {IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false})
            .SelectMany(type => type.GetInterfaces(), (type, interfaceType) => new {type, interfaceType})
            .Where(t => t.interfaceType.IsGenericType &&
                        t.interfaceType.GetGenericTypeDefinition() == typeof(IValidator<>));

        foreach (var validator in validators)
        {
            services.AddTransient(validator.interfaceType, validator.type);
        }
    }
}