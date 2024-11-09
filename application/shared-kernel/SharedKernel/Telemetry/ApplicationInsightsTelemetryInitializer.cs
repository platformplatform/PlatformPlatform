using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using PlatformPlatform.SharedKernel.ExecutionContext;

namespace PlatformPlatform.SharedKernel.Telemetry;

public class ApplicationInsightsTelemetryInitializer : ITelemetryInitializer
{
    private static readonly AsyncLocal<IExecutionContext> CurrentContext = new();

    public void Initialize(ITelemetry telemetry)
    {
        var executionContext = CurrentContext.Value;
        if (executionContext == null)
        {
            return;
        }

        if (executionContext.TenantId is not null)
        {
            telemetry.Context.User.AccountId = executionContext.TenantId.Value;
        }

        if (executionContext.UserInfo.UserId is not null)
        {
            telemetry.Context.User.Id = executionContext.UserInfo.UserId;
        }

        if (executionContext.UserInfo.IsAuthenticated)
        {
            telemetry.Context.User.AuthenticatedUserId = executionContext.UserInfo.UserId;
        }

        AddCustomProperty(telemetry, "user_Locale", executionContext.UserInfo.Locale);
        AddCustomProperty(telemetry, "user_Role", executionContext.UserInfo.UserRole);

        // If you have the user creation date in your execution context, you can set it like this:
        // The format should be ISO 8601: "2024-03-19T10:30:00.000Z"
        // telemetry.Context.User.AcquisitionDate = executionContext.UserInfo.CreatedDate.ToString("o");
    }

    public static void SetContext(IExecutionContext executionContext)
    {
        CurrentContext.Value = executionContext;
    }

    private static void AddCustomProperty(ITelemetry telemetry, string name, object? value)
    {
        var stringValue = value?.ToString();
        if (stringValue is null) return;
        telemetry.Context.GlobalProperties[name] = stringValue;
    }
}
