using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using NSwag.Annotations;
using StackFrame = Microsoft.ApplicationInsights.DataContracts.StackFrame;

namespace PlatformPlatform.SharedKernel.Endpoints;

public class TrackEndpoints : IEndpoints
{
    // <summary>
    //     Maps the track endpoints for usage of application insights in the web application
    //     Reason for this is to:
    //          * secure the instrumentation key
    //          * limit the amount of data that can be sent to application insights
    //          * allow IDE's to instrument and display telemetry data from the web application
    // </summary>
    public void MapEndpoints(IEndpointRouteBuilder routes)
    {
        routes.MapPost("/api/track", Track).AllowAnonymous().DisableAntiforgery();
    }

    [OpenApiIgnore]
    private static TrackResponse Track(
        HttpContext context,
        List<TrackRequest> trackRequests,
        TelemetryClient telemetryClient,
        ILogger<string> logger
    )
    {
        var ip = context.Connection.RemoteIpAddress?.ToString();
        foreach (var trackRequest in trackRequests)
        {
            switch (trackRequest.Data.BaseType)
            {
                case "PageviewData":
                {
                    var telemetry = new PageViewTelemetry
                    {
                        Name = trackRequest.Data.BaseData.Name,
                        Url = new Uri(trackRequest.Data.BaseData.Url),
                        Duration = trackRequest.Data.BaseData.Duration,
                        Timestamp = trackRequest.Time,
                        Id = trackRequest.Data.BaseData.Id
                    };

                    CopyContextTags(telemetry.Context, trackRequest.Tags, ip);
                    CopyDictionary(trackRequest.Data.BaseData.Properties, telemetry.Properties);
                    CopyDictionary(trackRequest.Data.BaseData.Measurements, telemetry.Metrics);

                    telemetryClient.TrackPageView(telemetry);
                    break;
                }
                case "PageviewPerformanceData":
                {
                    var telemetry = new PageViewPerformanceTelemetry
                    {
                        Name = trackRequest.Data.BaseData.Name,
                        Url = new Uri(trackRequest.Data.BaseData.Url),
                        Duration = trackRequest.Data.BaseData.Duration,
                        Timestamp = trackRequest.Time,
                        Id = trackRequest.Data.BaseData.Id,
                        PerfTotal = trackRequest.Data.BaseData.PerfTotal,
                        NetworkConnect = trackRequest.Data.BaseData.NetworkConnect,
                        SentRequest = trackRequest.Data.BaseData.SentRequest,
                        ReceivedResponse = trackRequest.Data.BaseData.ReceivedResponse,
                        DomProcessing = trackRequest.Data.BaseData.DomProcessing
                    };

                    CopyContextTags(telemetry.Context, trackRequest.Tags, ip);
                    CopyDictionary(trackRequest.Data.BaseData.Properties, telemetry.Properties);
                    CopyDictionary(trackRequest.Data.BaseData.Measurements, telemetry.Metrics);

                    telemetryClient.Track(telemetry);
                    break;
                }
                case "ExceptionData":
                {
                    var exceptionDetailsInfos = GetExceptionDetailsInfos(trackRequest);
                    var telemetry = new ExceptionTelemetry(exceptionDetailsInfos,
                        trackRequest.Data.BaseData.SeverityLevel, trackRequest.Data.BaseType,
                        trackRequest.Data.BaseData.Properties, new Dictionary<string, double>()
                    )
                    {
                        SeverityLevel = trackRequest.Data.BaseData.SeverityLevel,
                        Timestamp = trackRequest.Time
                    };

                    CopyContextTags(telemetry.Context, trackRequest.Tags, ip);
                    CopyDictionary(trackRequest.Data.BaseData.Properties, telemetry.Properties);
                    CopyDictionary(trackRequest.Data.BaseData.Measurements, telemetry.Metrics);

                    telemetryClient.TrackException(telemetry);
                    break;
                }
                case "MetricData":
                {
                    foreach (var metric in trackRequest.Data.BaseData.Metrics)
                    {
                        var telemetry = new MetricTelemetry
                        {
                            Name = metric.Name,
                            Sum = metric.Value,
                            Count = metric.Count,
                            Timestamp = trackRequest.Time
                        };

                        CopyContextTags(telemetry.Context, trackRequest.Tags, ip);
                        CopyDictionary(trackRequest.Data.BaseData.Properties, telemetry.Properties);

                        telemetryClient.TrackMetric(telemetry);
                    }

                    break;
                }
                case "RemoteDependencyData":
                {
                    // Ignore remote dependency data
                    break;
                }
                default:
                {
                    logger.LogWarning("Unsupported telemetry type: {BaseType}", trackRequest.Data.BaseType);
                    break;
                }
            }
        }

        return new TrackResponse(true, "Telemetry sent.");
    }

    private static IEnumerable<ExceptionDetailsInfo> GetExceptionDetailsInfos(TrackRequest trackRequest)
    {
        var exceptionDetailsInfos = trackRequest.Data.BaseData.Exceptions
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
                        )
                    )
                )
            );
        return exceptionDetailsInfos;
    }

    private static void CopyContextTags(TelemetryContext context, Dictionary<string, string> tags, string? ip)
    {
        context.Cloud.RoleInstance = tags.GetValueOrDefault("ai.cloud.roleInstance");
        context.Cloud.RoleName = tags.GetValueOrDefault("ai.cloud.roleName");

        context.Component.Version = tags.GetValueOrDefault("ai.application.ver");

        context.Device.Id = tags.GetValueOrDefault("ai.device.id");
        context.Device.Type = tags.GetValueOrDefault("ai.device.type");
        context.Device.Model = tags.GetValueOrDefault("ai.device.model");
        context.Device.OemName = tags.GetValueOrDefault("ai.device.oemName");
        context.Device.OperatingSystem = tags.GetValueOrDefault("ai.device.osVersion");

        context.Location.Ip = ip;

        context.User.Id = tags.GetValueOrDefault("ai.user.id");
        context.User.AccountId = tags.GetValueOrDefault("ai.user.accountId");

        context.Session.Id = tags.GetValueOrDefault("ai.session.id");

        context.Operation.Id = tags.GetValueOrDefault("ai.operation.id");
        context.Operation.Name = tags.GetValueOrDefault("ai.operation.name");
        context.Operation.ParentId = tags.GetValueOrDefault("ai.operation.parentId");
        context.Operation.CorrelationVector = tags.GetValueOrDefault("ai.operation.correlationVector");
        context.Operation.SyntheticSource = tags.GetValueOrDefault("ai.operation.syntheticSource");

        context.GetInternalContext().SdkVersion = tags.GetValueOrDefault("ai.internal.sdkVersion");
        context.GetInternalContext().AgentVersion = tags.GetValueOrDefault("ai.internal.agentVersion");
        context.GetInternalContext().NodeName = tags.GetValueOrDefault("ai.internal.nodeName");
    }

    private static void CopyDictionary<TValue>(IDictionary<string, TValue>? source, IDictionary<string, TValue> target)
    {
        if (source is null) return;

        foreach (var pair in source)
        {
            if (string.IsNullOrEmpty(pair.Key) || target.ContainsKey(pair.Key)) continue;
            target[pair.Key] = pair.Value;
        }
    }
}

[PublicAPI]
public record TrackResponse(bool Success, string Message);

[PublicAPI]
public record TrackRequest(
    DateTimeOffset Time,
    // ReSharper disable once InconsistentNaming
    string IKey,
    string Name,
    Dictionary<string, string> Tags,
    TrackData Data
);

[PublicAPI]
public record TrackData(string BaseType, TrackBaseData BaseData);

[PublicAPI]
public record TrackBaseData(
    string Name,
    string Url,
    TimeSpan Duration,
    TimeSpan PerfTotal,
    TimeSpan NetworkConnect,
    TimeSpan SentRequest,
    TimeSpan ReceivedResponse,
    TimeSpan DomProcessing,
    Dictionary<string, string> Properties,
    Dictionary<string, double> Measurements,
    List<TrackMetric> Metrics,
    List<TrackException> Exceptions,
    SeverityLevel SeverityLevel,
    string Id
);

[PublicAPI]
public record TrackMetric(string Name, int Kind, double Value, int Count);

[PublicAPI]
public record TrackException(string TypeName, string Message, bool HasFullStack, string Stack, List<TrackExceptionParsedStack> ParsedStack);

[PublicAPI]
public record TrackExceptionParsedStack(string Assembly, string FileName, string Method, int Line, int Level);
