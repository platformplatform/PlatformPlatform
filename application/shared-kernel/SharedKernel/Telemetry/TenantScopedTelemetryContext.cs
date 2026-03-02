using SharedKernel.Authentication;
using SharedKernel.Domain;
using SharedKernel.ExecutionContext;

namespace SharedKernel.Telemetry;

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
