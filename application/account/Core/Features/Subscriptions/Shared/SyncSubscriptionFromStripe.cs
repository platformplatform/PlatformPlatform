using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.Account.Integrations.Stripe;

namespace PlatformPlatform.Account.Features.Subscriptions.Shared;

/// <summary>
///     Shared helper that fetches current subscription and billing state from Stripe and applies it
///     to the local subscription aggregate. Mutates the aggregate in memory without persisting --
///     the caller is responsible for calling subscriptionRepository.Update().
/// </summary>
public sealed class SyncSubscriptionFromStripe(
    StripeClientFactory stripeClientFactory,
    ILogger<SyncSubscriptionFromStripe> logger
)
{
    public async Task<SyncResult> ExecuteAsync(Subscription subscription, CancellationToken cancellationToken)
    {
        if (subscription.StripeCustomerId is null)
        {
            logger.LogWarning("Cannot sync subscription '{SubscriptionId}' without Stripe customer ID", subscription.Id);
            return new SyncResult(false, null);
        }

        var stripeClient = stripeClientFactory.GetClient();

        var customerResult = await stripeClient.GetCustomerBillingInfoAsync(subscription.StripeCustomerId, cancellationToken);
        if (customerResult?.IsCustomerDeleted == true)
        {
            return new SyncResult(true, null);
        }

        var syncResult = await stripeClient.SyncSubscriptionStateAsync(subscription.StripeCustomerId, cancellationToken);
        string? subscriptionStatus = null;
        if (syncResult is not null)
        {
            subscription.SyncFromStripe(
                syncResult.Plan,
                syncResult.ScheduledPlan,
                syncResult.StripeSubscriptionId,
                syncResult.CurrentPriceAmount,
                syncResult.CurrentPriceCurrency,
                syncResult.CurrentPeriodEnd,
                syncResult.CancelAtPeriodEnd,
                [.. syncResult.PaymentTransactions],
                syncResult.PaymentMethod
            );
            subscriptionStatus = syncResult.SubscriptionStatus;
        }
        else
        {
            subscription.ResetToFreePlan();
        }

        subscription.SetBillingInfo(customerResult?.BillingInfo);

        return new SyncResult(false, subscriptionStatus);
    }
}

public sealed record SyncResult(bool IsCustomerDeleted, string? SubscriptionStatus);
