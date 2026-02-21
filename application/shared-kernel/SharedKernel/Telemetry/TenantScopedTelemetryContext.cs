using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.ExecutionContext;

namespace PlatformPlatform.SharedKernel.Telemetry;

public static class TenantScopedTelemetryContext
{
    public static void Set(TenantId tenantId, string? subscriptionPlan)
    {
        var userInfo = new UserInfo
        {
            IsAuthenticated = false,
            Locale = "en-US",
            SubscriptionPlan = subscriptionPlan,
            IsInternalUser = false
        };

        Activity.Current?.SetTag("tenant.id", tenantId.Value);
        Activity.Current?.SetTag("tenant.subscription_plan", subscriptionPlan);
        ApplicationInsightsTelemetryInitializer.SetContext(new BackgroundWorkerExecutionContext(tenantId, userInfo));
    }
}
