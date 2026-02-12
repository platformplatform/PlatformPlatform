using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.Account.Features.Tenants.Domain;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.Account.Integrations.Stripe;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.Account.Features.Subscriptions.Commands;

[PublicAPI]
public sealed record SyncSubscriptionCommand : ICommand, IRequest<Result>;

public sealed class SyncSubscriptionHandler(
    ISubscriptionRepository subscriptionRepository,
    ITenantRepository tenantRepository,
    StripeClientFactory stripeClientFactory,
    IExecutionContext executionContext,
    ITelemetryEventsCollector events
) : IRequestHandler<SyncSubscriptionCommand, Result>
{
    public async Task<Result> Handle(SyncSubscriptionCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result.Forbidden("Only owners can sync subscriptions.");
        }

        var subscription = await subscriptionRepository.GetByTenantIdAsync(cancellationToken);
        if (subscription is null)
        {
            return Result.NotFound("Subscription not found for current tenant.");
        }

        if (subscription.StripeCustomerId is null)
        {
            return Result.BadRequest("No Stripe customer linked to this subscription.");
        }

        var stripeClient = stripeClientFactory.GetClient();
        var syncResult = await stripeClient.SyncSubscriptionStateAsync(subscription.StripeCustomerId, cancellationToken);
        if (syncResult is null)
        {
            subscription.ResetToFreePlan();
        }
        else
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

        var billingInfo = await stripeClient.GetCustomerBillingInfoAsync(subscription.StripeCustomerId, cancellationToken);
        subscription.SetBillingInfo(billingInfo);

        subscription.ClearPaymentFailure();
        subscription.ClearDispute();
        subscription.ClearRefund();

        var tenant = await tenantRepository.GetCurrentTenantAsync(cancellationToken);
        if (tenant.State is TenantState.PastDue or TenantState.Suspended)
        {
            tenant.SetState(TenantState.Active);
            tenantRepository.Update(tenant);
        }

        subscriptionRepository.Update(subscription);

        events.CollectEvent(new SubscriptionSynced(subscription.Id, subscription.Plan));

        return Result.Success();
    }
}
