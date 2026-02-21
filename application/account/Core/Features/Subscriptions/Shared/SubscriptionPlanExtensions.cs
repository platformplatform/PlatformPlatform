using PlatformPlatform.Account.Features.Subscriptions.Domain;

namespace PlatformPlatform.Account.Features.Subscriptions.Shared;

public static class SubscriptionPlanExtensions
{
    extension(SubscriptionPlan target)
    {
        // Relies on enum ordering: higher values = higher-tier plans
        public bool IsUpgradeFrom(SubscriptionPlan current)
        {
            return target > current;
        }

        public bool IsDowngradeFrom(SubscriptionPlan current)
        {
            return target < current;
        }
    }
}
