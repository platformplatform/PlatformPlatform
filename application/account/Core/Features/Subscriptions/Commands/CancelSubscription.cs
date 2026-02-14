using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.Account.Integrations.Stripe;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.Account.Features.Subscriptions.Commands;

[PublicAPI]
public sealed record CancelSubscriptionCommand(CancellationReason Reason, string? Feedback) : ICommand, IRequest<Result>;

public sealed class CancelSubscriptionHandler(
    ISubscriptionRepository subscriptionRepository,
    StripeClientFactory stripeClientFactory,
    IExecutionContext executionContext,
    ITelemetryEventsCollector events,
    ILogger<CancelSubscriptionHandler> logger
) : IRequestHandler<CancelSubscriptionCommand, Result>
{
    public async Task<Result> Handle(CancelSubscriptionCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result.Forbidden("Only owners can manage subscriptions.");
        }

        var subscription = await subscriptionRepository.GetByTenantIdAsync(cancellationToken)
                           ?? throw new UnreachableException($"Subscription not found for tenant '{executionContext.TenantId}'.");

        if (subscription.Plan == SubscriptionPlan.Basis)
        {
            return Result.BadRequest("Cannot cancel a Basis subscription.");
        }

        if (subscription.StripeSubscriptionId is null)
        {
            logger.LogWarning("No Stripe subscription found for subscription '{SubscriptionId}'", subscription.Id);
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

        subscription.SetCancellationFeedback(command.Reason, command.Feedback);
        subscriptionRepository.Update(subscription);

        events.CollectEvent(new SubscriptionCancelled(subscription.Id, subscription.Plan, command.Reason, command.Feedback));

        return Result.Success();
    }
}
