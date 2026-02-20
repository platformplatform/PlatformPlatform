using Microsoft.ApplicationInsights;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.SharedKernel.PipelineBehaviors;

public sealed class PublishTelemetryEventsPipelineBehavior<TRequest, TResponse>(
    ITelemetryEventsCollector telemetryEventsCollector,
    TelemetryClient telemetryClient,
    ConcurrentCommandCounter concurrentCommandCounter,
    ILogger<PublishTelemetryEventsPipelineBehavior<TRequest, TResponse>> logger
) : IPipelineBehavior<TRequest, TResponse> where TRequest : ICommand where TResponse : ResultBase
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next(cancellationToken);

        if (concurrentCommandCounter.IsZero())
        {
            while (telemetryEventsCollector.HasEvents)
            {
                var telemetryEvent = telemetryEventsCollector.Dequeue();

                telemetryClient.TrackEvent(telemetryEvent.GetType().Name, telemetryEvent.Properties);
                logger.LogInformation("Telemetry: {EventName} {EventProperties}", telemetryEvent.GetType().Name, string.Join(", ", telemetryEvent.Properties.Select(p => $"{p.Key}={p.Value}")));
            }
        }

        return response;
    }
}
