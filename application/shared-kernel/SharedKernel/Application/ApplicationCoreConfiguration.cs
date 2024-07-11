using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.SharedKernel.Application.Behaviors;
using PlatformPlatform.SharedKernel.Application.TelemetryEvents;

namespace PlatformPlatform.SharedKernel.Application;

public static class ApplicationConfiguration
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, Assembly applicationAssembly)
    {
        // Order is important! First all Pre behaviors run, then the command is handled, then all Post behaviors run.
        // So Validation -> Command -> PublishDomainEvents -> UnitOfWork -> PublishTelemetryEvents.
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehavior<,>)); // Pre
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PublishTelemetryEventsPipelineBehavior<,>)); // Post
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkPipelineBehavior<,>)); // Post
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PublishDomainEventsPipelineBehavior<,>)); // Post
        services.AddScoped<ITelemetryEventsCollector, TelemetryEventsCollector>();
        services.AddScoped<ConcurrentCommandCounter>();

        services.AddMediatR(configuration => configuration.RegisterServicesFromAssemblies(applicationAssembly));
        services.AddValidatorsFromAssembly(applicationAssembly);

        return services;
    }
}
