using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.AccountManagement.Application.Behaviours;

namespace PlatformPlatform.AccountManagement.Application;

public static class ApplicationConfiguration
{
    public static readonly Assembly Assembly = Assembly.GetExecutingAssembly();

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddMediatR(configuration => configuration.RegisterServicesFromAssemblies(Assembly));

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>));

        services.AddValidatorsFromAssembly(Assembly);

        return services;
    }
}