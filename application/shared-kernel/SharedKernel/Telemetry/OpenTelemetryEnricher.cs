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
            Activity.Current.SetTag("enduser.id", executionContext.UserInfo.UserId);
        }

        // Set custom properties, ensure any changes here are also added to ApplicationInsightsTelemetryInitializer
        Activity.Current.SetTag("tenant_id", executionContext.TenantId?.Value);
        Activity.Current.SetTag("user_id", executionContext.UserInfo.UserId);
        Activity.Current.SetTag("user_IsAuthenticated", executionContext.UserInfo.IsAuthenticated);
        Activity.Current.SetTag("user_Locale", executionContext.UserInfo.Locale);
        Activity.Current.SetTag("user_Role", executionContext.UserInfo.UserRole);
    }
}
