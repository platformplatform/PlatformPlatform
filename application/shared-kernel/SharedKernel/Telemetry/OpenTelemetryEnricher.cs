using SharedKernel.ExecutionContext;

namespace SharedKernel.Telemetry;

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
        Activity.Current.SetTag("tenant.subscription_plan", executionContext.UserInfo.SubscriptionPlan);
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

        // Iteration is over current C# definitions only; orphaned flag keys (DB rows whose key was removed
        // from FeatureFlags.cs) cannot reach telemetry because FeatureFlagDefinitionReconciler marks them
        // OrphanedAt at startup and they are no longer in GetAll(). If a future change loads flags from the
        // database instead of definitions, the orphan filter must be re-introduced here explicitly.
        foreach (var featureFlag in FeatureFlags.FeatureFlags.GetAll())
        {
            if (!featureFlag.TrackInTelemetry) continue;
            var telemetryName = featureFlag.TelemetryName ?? featureFlag.Key;
            var value = executionContext.UserInfo.FeatureFlags.Contains(featureFlag.Key) ? "enabled" : "disabled";
            Activity.Current.SetTag($"feature_{telemetryName}", value);
        }
    }
}
