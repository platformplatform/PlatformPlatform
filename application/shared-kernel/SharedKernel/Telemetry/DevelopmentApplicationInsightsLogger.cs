using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.DependencyInjection;

namespace PlatformPlatform.SharedKernel.Telemetry;

public sealed class DevelopmentApplicationInsightsLogger(ITelemetryProcessor next, IServiceProvider serviceProvider) : ITelemetryProcessor
{
    private ILogger Logger => field ??= serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<DevelopmentApplicationInsightsLogger>();

    public void Process(ITelemetry item)
    {
        if (item is EventTelemetry or RequestTelemetry or DependencyTelemetry)
        {
            var properties = new Dictionary<string, object?>
            {
                ["ai.operation.id"] = item.Context.Operation.Id,
                ["ai.operation.parentId"] = item.Context.Operation.ParentId,
                ["ai.operation.name"] = item.Context.Operation.Name,
                ["ai.cloud.roleName"] = item.Context.Cloud.RoleName,
                ["ai.cloud.roleInstance"] = item.Context.Cloud.RoleInstance,
                ["ai.application.ver"] = item.Context.Component.Version,
                ["ai.device.type"] = item.Context.Device.Type,
                ["ai.device.os"] = item.Context.Device.OperatingSystem,
                ["ai.location.ip"] = item.Context.Location.Ip,
                ["ai.user.id"] = item.Context.User.Id,
                ["ai.user.accountId"] = item.Context.User.AccountId,
                ["ai.user.authenticatedUserId"] = item.Context.User.AuthenticatedUserId,
                ["ai.session.id"] = item.Context.Session.Id
            };

            foreach (var prop in item.Context.GlobalProperties)
            {
                properties[prop.Key] = prop.Value;
            }

            switch (item)
            {
                case EventTelemetry eventTelemetry:
                    foreach (var prop in eventTelemetry.Properties)
                    {
                        properties[prop.Key] = prop.Value;
                    }

                    break;
                case RequestTelemetry requestTelemetry:
                    properties["request.url"] = requestTelemetry.Url?.ToString();
                    properties["request.responseCode"] = requestTelemetry.ResponseCode;
                    properties["request.duration"] = requestTelemetry.Duration.ToString();
                    properties["request.success"] = requestTelemetry.Success;
                    break;
                case DependencyTelemetry dependencyTelemetry:
                    properties["dependency.type"] = dependencyTelemetry.Type;
                    properties["dependency.target"] = dependencyTelemetry.Target;
                    properties["dependency.data"] = dependencyTelemetry.Data;
                    properties["dependency.success"] = dependencyTelemetry.Success;
                    break;
            }

            var cleanProperties = properties.Where(p => p.Value is not null).ToDictionary(p => p.Key, p => p.Value!);

            var telemetryType = item.GetType().Name.Replace("Telemetry", "");
            var itemName = item switch
            {
                EventTelemetry e => e.Name,
                RequestTelemetry r => r.Name,
                DependencyTelemetry d => d.Name,
                _ => telemetryType
            };

            using (Logger.BeginScope(cleanProperties))
            {
                Logger.LogInformation("[{TelemetryType}] {ItemName}", telemetryType, itemName);
            }
        }

        next.Process(item);
    }
}
