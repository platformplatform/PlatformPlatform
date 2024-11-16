using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.SharedKernel.ExecutionContext;

namespace PlatformPlatform.SharedKernel.Configuration;

public static class WorkerDependencyConfiguration
{
    public static IServiceCollection AddWorkerServices(this IServiceCollection services)
    {
        // Add the execution context service that will be used to make current user information available to the application
        return services.AddScoped<IExecutionContext, BackgroundWorkerExecutionContext>();
    }
}
