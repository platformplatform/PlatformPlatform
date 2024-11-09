using Microsoft.ApplicationInsights;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.TelemetryEvents;

namespace PlatformPlatform.SharedKernel.Behaviors;

public sealed class PublishTelemetryEventsPipelineBehavior<TRequest, TResponse>(
    ITelemetryEventsCollector telemetryEventsCollector,
    TelemetryClient telemetryClient,
    ConcurrentCommandCounter concurrentCommandCounter,
    IExecutionContext executionContext
) : IPipelineBehavior<TRequest, TResponse> where TRequest : ICommand where TResponse : ResultBase
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();

        if (concurrentCommandCounter.IsZero())
        {
            while (telemetryEventsCollector.HasEvents)
            {
                var telemetryEvent = telemetryEventsCollector.Dequeue();

                AddPropertyIfNotNull(telemetryEvent, "tenant_Id", executionContext.TenantId);
                AddPropertyIfNotNull(telemetryEvent, "user_IsAuthenticated", executionContext.UserInfo.IsAuthenticated.ToString());
                AddPropertyIfNotNull(telemetryEvent, "user_Id", executionContext.UserInfo.UserId);
                AddPropertyIfNotNull(telemetryEvent, "user_Locale", executionContext.UserInfo.Locale);
                AddPropertyIfNotNull(telemetryEvent, "user_Role", executionContext.UserInfo.UserRole);

                telemetryClient.TrackEvent(telemetryEvent.GetType().Name, telemetryEvent.Properties);
            }
        }

        return response;
    }

    private static void AddPropertyIfNotNull(TelemetryEvent telemetryEvent, string name, object? value)
    {
        var stringValue = value?.ToString();
        if (stringValue is null) return;
        telemetryEvent.Properties.Add(name, stringValue);
    }
}
