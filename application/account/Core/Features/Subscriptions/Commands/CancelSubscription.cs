using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.Account.Integrations.Stripe;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.Account.Features.Subscriptions.Commands;

[PublicAPI]
public sealed record CancelSubscriptionCommand : ICommand, IRequest<Result>;

public sealed class CancelSubscriptionHandler(
    ISubscriptionRepository subscriptionRepository,
    StripeClientFactory stripeClientFactory,
    IExecutionContext executionContext,
    ITelemetryEventsCollector events
) : IRequestHandler<CancelSubscriptionCommand, Result>
{
    public async Task<Result> Handle(CancelSubscriptionCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result.Forbidden("Only owners can manage subscriptions.");
        }

        var subscription = await subscriptionRepository.GetByTenantIdAsync(cancellationToken);
        if (subscription is null)
        {
            return Result.NotFound("Subscription not found for current tenant.");
        }

        if (subscription.Plan == SubscriptionPlan.Trial)
        {
            return Result.BadRequest("Cannot cancel a Trial subscription.");
        }

        if (subscription.StripeSubscriptionId is null)
        {
            return Result.BadRequest("No active Stripe subscription found.");
        }

        if (subscription.CancelAtPeriodEnd)
        {
            return Result.BadRequest("Subscription is already scheduled for cancellation.");
        }

        var stripeClient = stripeClientFactory.GetClient();
        var success = await stripeClient.CancelSubscriptionAtPeriodEndAsync(subscription.StripeSubscriptionId, cancellationToken);
        if (!success)
        {
            return Result.BadRequest("Failed to cancel subscription in Stripe.");
        }

        var syncResult = await stripeClient.SyncSubscriptionStateAsync(subscription.StripeCustomerId!, cancellationToken);
        if (syncResult is not null)
        {
            subscription.SyncFromStripe(syncResult.Plan, syncResult.ScheduledPlan, syncResult.StripeSubscriptionId, syncResult.CurrentPeriodEnd, syncResult.CancelAtPeriodEnd, [.. syncResult.PaymentTransactions]);
        }

        subscriptionRepository.Update(subscription);

        events.CollectEvent(new SubscriptionCancelled(subscription.Id, subscription.Plan));

        return Result.Success();
    }
}
