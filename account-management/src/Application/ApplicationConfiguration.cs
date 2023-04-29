using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.AccountManagement.Application.Shared;
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

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkPipelineBehavior<,>));

        services.RegisterCommandsWithValidationPipelineBehavior();

        services.AddValidatorsFromAssembly(Assembly);

        return services;
    }

    /// <summary>
    ///     Finds all Command that implement IRequestHandler<,> and registers a validation pipeline for each of them.
    ///     This uses a lot of reflection, but it is only called once at startup. And saves us from having to register
    ///     each Command manually for validation.
    /// </summary>
    private static IServiceCollection RegisterCommandsWithValidationPipelineBehavior(this IServiceCollection services)
    {
        // Fetch all types that implement IRequestHandler<,>
        var handlerTypes = typeof(ApplicationConfiguration).Assembly
            .GetExportedTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.GetInterfaces().Any(i =>
                i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)));

        foreach (var handlerType in handlerTypes)
        {
            // Find the IRequestHandler<,> interface
            var requestHandlerInterface = handlerType.GetInterfaces()
                .Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>));
            // Get the generic arguments of the IRequestHandler<,> interface
            var genericArguments = requestHandlerInterface.GetGenericArguments();
            // The first generic argument is the command type
            var commandType = genericArguments[0];
            // The second generic argument is the result type
            var resultType = genericArguments[1];
            // The first generic argument of the result type is the response type
            var responseType = resultType.GetGenericArguments()[0];
            // The RegisterValidationPipelineForCommand method is a generic method, so we need to use reflection
            var registerMethod = typeof(ApplicationConfiguration).GetMethod(
                nameof(RegisterValidationPipelineForCommand), BindingFlags.NonPublic | BindingFlags.Static);
            // Invoke the RegisterValidationPipelineForCommand method with the command type and response type
            registerMethod!.MakeGenericMethod(commandType, responseType).Invoke(null, new object[] {services});
        }

        return services;
    }

    private static void RegisterValidationPipelineForCommand<TCommand, TResponse>(IServiceCollection services)
        where TCommand : IRequest<Result<TResponse>>
    {
        services
            .AddTransient<IPipelineBehavior<TCommand, Result<TResponse>>,
                ValidationPipelineBehavior<TCommand, TResponse>>();
    }
}