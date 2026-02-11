using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.Account.Integrations.Stripe;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.Account.Features.Subscriptions.Commands;

[PublicAPI]
public sealed record CancelScheduledDowngradeCommand : ICommand, IRequest<Result>;

public sealed class CancelScheduledDowngradeHandler(
    ISubscriptionRepository subscriptionRepository,
    StripeClientFactory stripeClientFactory,
    IExecutionContext executionContext,
    ITelemetryEventsCollector events
) : IRequestHandler<CancelScheduledDowngradeCommand, Result>
{
    public async Task<Result> Handle(CancelScheduledDowngradeCommand command, CancellationToken cancellationToken)
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

        if (subscription.StripeSubscriptionId is null)
        {
            return Result.BadRequest("No active Stripe subscription found.");
        }

        if (subscription.ScheduledPlan is null)
        {
            return Result.BadRequest("No scheduled downgrade to cancel.");
        }

        var scheduledPlan = subscription.ScheduledPlan.Value;
        var stripeClient = stripeClientFactory.GetClient();
        var success = await stripeClient.CancelScheduledDowngradeAsync(subscription.StripeSubscriptionId, cancellationToken);
        if (!success)
        {
            return Result.BadRequest("Failed to cancel scheduled downgrade in Stripe.");
        }

        events.CollectEvent(new SubscriptionDowngradeCancelled(subscription.Id, subscription.Plan, scheduledPlan));

        return Result.Success();
    }
}
