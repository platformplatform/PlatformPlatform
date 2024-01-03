using System.Globalization;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using StackFrame = Microsoft.ApplicationInsights.DataContracts.StackFrame;

namespace PlatformPlatform.SharedKernel.ApiCore.Endpoints;

public static class TrackEndpoints
{
    public static void MapTrackEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapPost("/api/track", Track);
    }

    private static TrackResponseSuccessDto Track(
        List<TrackRequestDto> trackRequests,
        TelemetryClient telemetryClient,
        ILogger<string> logger
    )
    {
        foreach (var trackRequestDto in trackRequests)
        {
            switch (trackRequestDto.Data.BaseType)
            {
                case "PageviewData":
                {
                    var telemetry = new PageViewTelemetry
                    {
                        Name = trackRequestDto.Data.BaseData.Name,
                        Url = new Uri(trackRequestDto.Data.BaseData.Url),
                        Duration = TimeSpan.Parse(trackRequestDto.Data.BaseData.Duration, CultureInfo.InvariantCulture),
                        Timestamp = DateTimeOffset.Parse(trackRequestDto.Time, CultureInfo.InvariantCulture),
                        Id = trackRequestDto.Data.BaseData.Id
                    };

                    CopyDictionary(trackRequestDto.Data.BaseData.Properties, telemetry.Properties);
                    CopyDictionary(trackRequestDto.Data.BaseData.Measurements, telemetry.Metrics);

                    telemetryClient.TrackPageView(telemetry);
                    break;
                }
                case "PageviewPerformanceData":
                {
                    var telemetry = new PageViewPerformanceTelemetry
                    {
                        Name = trackRequestDto.Data.BaseData.Name,
                        Url = new Uri(trackRequestDto.Data.BaseData.Url),
                        Duration = TimeSpan.Parse(trackRequestDto.Data.BaseData.Duration, CultureInfo.InvariantCulture),
                        Timestamp = DateTimeOffset.Parse(trackRequestDto.Time, CultureInfo.InvariantCulture),
                        Id = trackRequestDto.Data.BaseData.Id
                    };

                    CopyDictionary(trackRequestDto.Data.BaseData.Properties, telemetry.Properties);
                    CopyDictionary(trackRequestDto.Data.BaseData.Measurements, telemetry.Metrics);

                    telemetryClient.Track(telemetry);
                    break;
                }
                case "ExceptionData":
                {
                    var exceptionDetailsInfos = GetExceptionDetailsInfos(trackRequestDto);
                    var telemetry = new ExceptionTelemetry(exceptionDetailsInfos,
                        trackRequestDto.Data.BaseData.SeverityLevel, trackRequestDto.Data.BaseType,
                        trackRequestDto.Data.BaseData.Properties, new Dictionary<string, double>())
                    {
                        SeverityLevel = trackRequestDto.Data.BaseData.SeverityLevel,
                        Timestamp = DateTimeOffset.Parse(trackRequestDto.Time, CultureInfo.InvariantCulture)
                    };

                    CopyDictionary(trackRequestDto.Data.BaseData.Properties, telemetry.Properties);
                    CopyDictionary(trackRequestDto.Data.BaseData.Measurements, telemetry.Metrics);

                    telemetryClient.TrackException(telemetry);
                    break;
                }
                case "MetricData":
                {
                    var metric = trackRequestDto.Data.BaseData.Metrics[0];
                    var telemetry = new MetricTelemetry
                    {
                        Name = metric.Name,
                        Sum = metric.Value,
                        Count = metric.Count,
                        Timestamp = DateTimeOffset.Parse(trackRequestDto.Time, CultureInfo.InvariantCulture)
                    };

                    CopyDictionary(trackRequestDto.Data.BaseData.Properties, telemetry.Properties);

                    telemetryClient.TrackMetric(telemetry);
                    break;
                }
                case "RemoteDependencyData":
                    // Ignore remote dependency data
                    break;
                default:
                    logger.LogWarning($"Unsupported telemetry type: {trackRequestDto.Data.BaseType}");
                    break;
            }
        }

        return new TrackResponseSuccessDto(true, "Telemetry sent.");
    }

    private static IEnumerable<ExceptionDetailsInfo> GetExceptionDetailsInfos(TrackRequestDto trackRequestDto)
    {
        var exceptionDetailsInfos = trackRequestDto.Data.BaseData.Exceptions
            .Select(exception => new ExceptionDetailsInfo(
                0,
                0,
                exception.TypeName,
                exception.Message,
                exception.HasFullStack,
                exception.Stack,
                exception.ParsedStack.Select(parsedStack => new StackFrame(
                    parsedStack.Assembly,
                    parsedStack.FileName,
                    parsedStack.Level,
                    parsedStack.Line,
                    parsedStack.Method
                ))
            ));
        return exceptionDetailsInfos;
    }

    private static void CopyDictionary<TValue>(IDictionary<string, TValue>? source, IDictionary<string, TValue> target)
    {
        if (source == null) return;

        foreach (var pair in source)
        {
            if (string.IsNullOrEmpty(pair.Key) || target.ContainsKey(pair.Key)) continue;
            target[pair.Key] = pair.Value;
        }
    }
}

[UsedImplicitly]
public record TrackResponseSuccessDto(bool Success, string Message);

[UsedImplicitly]
public record TrackRequestDto(
    string Time,
    // ReSharper disable once InconsistentNaming
    string IKey,
    string Name,
    Dictionary<string, string> Tags,
    TrackRequestDataDto Data
);

[UsedImplicitly]
public record TrackRequestDataDto(string BaseType, TrackRequestBaseDataDto BaseData);

[UsedImplicitly]
public record TrackRequestBaseDataDto(
    string Name,
    string Url,
    string Duration,
    Dictionary<string, string> Properties,
    Dictionary<string, double> Measurements,
    List<TrackRequestMetricsDto> Metrics,
    List<TrackRequestExceptionDto> Exceptions,
    SeverityLevel SeverityLevel,
    string Id
);

[UsedImplicitly]
public record TrackRequestMetricsDto(string Name, int Kind, double Value, int Count);

[UsedImplicitly]
public record TrackRequestExceptionDto(
    string TypeName,
    string Message,
    bool HasFullStack,
    string Stack,
    List<TrackRequestParsedStackDto> ParsedStack
);

[UsedImplicitly]
public record TrackRequestParsedStackDto(string Assembly, string FileName, string Method, int Line, int Level);