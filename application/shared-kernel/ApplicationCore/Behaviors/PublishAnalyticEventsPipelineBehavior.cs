using Microsoft.ApplicationInsights;
using PlatformPlatform.SharedKernel.ApplicationCore.Cqrs;
using PlatformPlatform.SharedKernel.ApplicationCore.Tracking;

namespace PlatformPlatform.SharedKernel.ApplicationCore.Behaviors;

public sealed class PublishAnalyticEventsPipelineBehavior<TRequest, TResponse>(
    IAnalyticEventsCollector analyticEventsCollector,
    TelemetryClient telemetryClient
)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : ICommand where TResponse : ResultBase
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        var result = await next();

        while (analyticEventsCollector.HasEvents)
        {
            var analyticEvent = analyticEventsCollector.Dequeue();
            telemetryClient.TrackEvent(analyticEvent.Name, analyticEvent.Properties);
        }

        return result;
    }
}