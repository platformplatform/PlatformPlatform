using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace PlatformPlatform.SharedKernel.ApiCore.Filters;

/// <summary>
///     Filter out telemetry from requests matching excluded paths
/// </summary>
[UsedImplicitly]
public class EndpointTelemetryFilter(ITelemetryProcessor telemetryProcessor) : ITelemetryProcessor
{
    private readonly string[] _excludedPaths = { "/swagger", "/health", "/alive", "/track" };

    public void Process(ITelemetry item)
    {
        if (item is RequestTelemetry requestTelemetry && IsExcludedPath(requestTelemetry))
        {
            return;
        }

        telemetryProcessor.Process(item);
    }

    private bool IsExcludedPath(RequestTelemetry requestTelemetry)
    {
        return Array.Exists(_excludedPaths, excludePath => requestTelemetry.Url.AbsolutePath.StartsWith(excludePath));
    }
}