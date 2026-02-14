using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.Account.Integrations.Stripe;

namespace PlatformPlatform.Account.Features.Subscriptions.Shared;

/// <summary>
///     Shared helper that fetches current subscription and billing state from Stripe and applies it
///     to the local subscription aggregate. Used by user-action commands for immediate consistency
///     and by the webhook processor for eventual consistency.
/// </summary>
public sealed class SyncSubscriptionFromStripe(
    ISubscriptionRepository subscriptionRepository,
    StripeClientFactory stripeClientFactory,
    ILogger<SyncSubscriptionFromStripe> logger
)
{
    public async Task<bool> ExecuteAsync(Subscription subscription, CancellationToken cancellationToken)
    {
        if (subscription.StripeCustomerId is null)
        {
            logger.LogWarning("Cannot sync subscription '{SubscriptionId}' without Stripe customer ID", subscription.Id);
            return false;
        }

        var stripeClient = stripeClientFactory.GetClient();
        var syncResult = await stripeClient.SyncSubscriptionStateAsync(subscription.StripeCustomerId, cancellationToken);
        if (syncResult is not null)
        {
            subscription.SyncFromStripe(
                syncResult.Plan,
                syncResult.ScheduledPlan,
                syncResult.StripeSubscriptionId,
                syncResult.CurrentPeriodEnd,
                syncResult.CancelAtPeriodEnd,
                [.. syncResult.PaymentTransactions],
                syncResult.PaymentMethod
            );
        }
        else
        {
            subscription.ResetToFreePlan();
        }

        var billingInfo = await stripeClient.GetCustomerBillingInfoAsync(subscription.StripeCustomerId, cancellationToken);
        subscription.SetBillingInfo(billingInfo);

        subscriptionRepository.Update(subscription);

        return true;
    }
}
