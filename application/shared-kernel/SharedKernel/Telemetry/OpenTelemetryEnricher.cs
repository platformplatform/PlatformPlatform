using PlatformPlatform.SharedKernel.ExecutionContext;

namespace PlatformPlatform.SharedKernel.Telemetry;

public class OpenTelemetryEnricher(IExecutionContext executionContext)
{
    public void Apply()
    {
        if (Activity.Current is null) return;

        // Set standard OpenTelemetry semantic convention for getting the geo data from the Client IP Address
        Activity.Current.SetTag("client.address", executionContext.ClientIpAddress.ToString());

        if (executionContext.UserInfo.IsAuthenticated)
        {
            Activity.Current.SetTag("enduser.id", executionContext.UserInfo.Id);
        }

        // Set custom properties, ensure any changes here are also added to ApplicationInsightsTelemetryInitializer
        Activity.Current.SetTag("tenant.id", executionContext.TenantId?.Value);
        Activity.Current.SetTag("user.id", executionContext.UserInfo.Id);
        Activity.Current.SetTag("user.is_authenticated", executionContext.UserInfo.IsAuthenticated);
        Activity.Current.SetTag("user.locale", executionContext.UserInfo.Locale);
        if (executionContext.UserInfo.ZoomLevel is not null)
        {
            Activity.Current.SetTag("user.zoom_level", executionContext.UserInfo.ZoomLevel);
        }

        if (executionContext.UserInfo.Theme is not null)
        {
            Activity.Current.SetTag("user.theme", executionContext.UserInfo.Theme);
        }

        Activity.Current.SetTag("user.role", executionContext.UserInfo.Role);
        Activity.Current.SetTag("user.session_id", executionContext.UserInfo.SessionId?.Value);
    }
}
