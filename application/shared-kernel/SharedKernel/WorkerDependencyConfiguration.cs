using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.SharedKernel.ExecutionContext;

namespace PlatformPlatform.SharedKernel;

public static class WorkerDependencyConfiguration
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        return services.AddWorkerExecutionContext();
    }

    public static IServiceCollection AddWorkerExecutionContext(this IServiceCollection services)
    {
        // Add the execution context service that will be used to make current user information available to the application
        return services.AddScoped<IExecutionContext, BackgroundWorkerExecutionContext>();
    }
}
