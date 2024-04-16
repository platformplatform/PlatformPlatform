using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace PlatformPlatform.SharedKernel.ApiCore.Filters;

/// <summary>
///     Filter out telemetry from requests matching excluded paths
/// </summary>
public class EndpointTelemetryFilter(ITelemetryProcessor telemetryProcessor) : ITelemetryProcessor
{
    public static readonly string[] ExcludedPaths = ["/swagger", "/health", "/alive", "/api/track"];
    
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
        return Array.Exists(ExcludedPaths, excludePath => requestTelemetry.Url.AbsolutePath.StartsWith(excludePath));
    }
}
