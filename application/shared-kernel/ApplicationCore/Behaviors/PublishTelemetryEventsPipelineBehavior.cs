using Microsoft.ApplicationInsights;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;
using PlatformPlatform.SharedKernel.ApplicationCore.TelemetryEvents;

namespace PlatformPlatform.SharedKernel.ApplicationCore.Behaviors;

public sealed class PublishTelemetryEventsPipelineBehavior<TRequest, TResponse>(
    ITelemetryEventsCollector telemetryEventsCollector,
    TelemetryClient telemetryClient,
    ConcurrentCommandCounter concurrentCommandCounter
) : IPipelineBehavior<TRequest, TResponse> where TRequest : ICommand where TResponse : ResultBase
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        var response = await next();

        if (concurrentCommandCounter.IsZero())
        {
            while (telemetryEventsCollector.HasEvents)
            {
                var telemetryEvent = telemetryEventsCollector.Dequeue();
                telemetryClient.TrackEvent(telemetryEvent.Name, telemetryEvent.Properties);
            }
        }

        return response;
    }
}