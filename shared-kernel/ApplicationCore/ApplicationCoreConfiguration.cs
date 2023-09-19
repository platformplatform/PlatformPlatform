using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.SharedKernel.ApplicationCore.Behaviors;

namespace PlatformPlatform.SharedKernel.ApplicationCore;

public static class ApplicationCoreConfiguration
{
    [UsedImplicitly]
    public static IServiceCollection AddApplicationCoreServices(this IServiceCollection services,
        Assembly applicationAssembly)
    {
        // Order is important. First all Pre behaviors run (top to bottom), then the command is handled, then all Post
        // behaviors run (bottom to top). So Validation -> Command -> PublishDomainEvents -> UnitOfWork.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehavior<,>)); // Pre
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkPipelineBehavior<,>)); // Post
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PublishDomainEventsPipelineBehavior<,>)); // Post
        services.AddScoped<UnitOfWorkPipelineBehaviorConcurrentCounter>();

        services.AddMediatR(configuration => configuration.RegisterServicesFromAssemblies(applicationAssembly));
        services.AddNonGenericValidators(applicationAssembly);

        return services;
    }

    /// <summary>
    ///     Registers all non-generic and non-abstract validators in the specified assembly. This is necessary because
    ///     services.AddValidatorsFromAssembly() includes registration of generic and abstract validators.
    /// </summary>
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