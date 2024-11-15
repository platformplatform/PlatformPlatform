using System.Collections.Immutable;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace PlatformPlatform.SharedKernel.Telemetry;

/// <summary>
///     Filter out telemetry from requests matching excluded paths
/// </summary>
public class EndpointTelemetryFilter(ITelemetryProcessor telemetryProcessor)
    : ITelemetryProcessor
{
    public static readonly ImmutableHashSet<string> ExcludedPaths = ImmutableHashSet.Create(
        StringComparer.OrdinalIgnoreCase,
        "/swagger", "/internal-api/live", "/internal-api/ready", "/api/track"
    );

    public static readonly ImmutableHashSet<string> ExcludedFileExtensions = ImmutableHashSet.Create(
        StringComparer.OrdinalIgnoreCase,
        ".js", ".css", ".png", ".jpg", ".ico", ".map", ".svg", ".woff", ".woff2", "webp"
    );

    public void Process(ITelemetry item)
    {
        if (item is RequestTelemetry requestTelemetry && (IsExcludedPath(requestTelemetry) || IsExcludedFileExtension(requestTelemetry)))
        {
            return;
        }

        telemetryProcessor.Process(item);
    }

    private static bool IsExcludedPath(RequestTelemetry requestTelemetry)
    {
        var path = requestTelemetry.Url.AbsolutePath;
        return ExcludedPaths.Any(excludePath => path.StartsWith(excludePath));
    }

    private static bool IsExcludedFileExtension(RequestTelemetry requestTelemetry)
    {
        var path = requestTelemetry.Url.AbsolutePath;
        return ExcludedFileExtensions.Any(excludeExtension => path.EndsWith(excludeExtension));
    }
}
