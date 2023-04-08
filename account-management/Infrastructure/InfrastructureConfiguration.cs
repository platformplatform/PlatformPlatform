using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace PlatformPlatform.AccountManagement.Infrastructure;

public static class InfrastructureConfiguration
{
    public static readonly Assembly Assembly = typeof(InfrastructureConfiguration).Assembly;

    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        return services;
    }
}