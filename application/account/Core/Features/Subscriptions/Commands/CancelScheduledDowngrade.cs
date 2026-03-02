using Account.Features.Subscriptions.Domain;
using Account.Features.Users.Domain;
using Account.Integrations.Stripe;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.ExecutionContext;

namespace Account.Features.Subscriptions.Commands;

[PublicAPI]
public sealed record CancelScheduledDowngradeCommand : ICommand, IRequest<Result>;

public sealed class CancelScheduledDowngradeHandler(
    ISubscriptionRepository subscriptionRepository,
    StripeClientFactory stripeClientFactory,
    IExecutionContext executionContext,
    ILogger<CancelScheduledDowngradeHandler> logger
) : IRequestHandler<CancelScheduledDowngradeCommand, Result>
{
    public async Task<Result> Handle(CancelScheduledDowngradeCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result.Forbidden("Only owners can manage subscriptions.");
        }

        var subscription = await subscriptionRepository.GetCurrentAsync(cancellationToken);

        if (subscription.StripeSubscriptionId is null)
        {
            logger.LogWarning("No Stripe subscription found for subscription '{SubscriptionId}'", subscription.Id);
            return Result.BadRequest("No active Stripe subscription found.");
        }

        if (subscription.ScheduledPlan is null)
        {
            return Result.BadRequest("No scheduled downgrade to cancel.");
        }

        var stripeClient = stripeClientFactory.GetClient();
        var success = await stripeClient.CancelScheduledDowngradeAsync(subscription.StripeSubscriptionId, cancellationToken);
        if (!success)
        {
            return Result.BadRequest("Failed to cancel scheduled downgrade in Stripe.");
        }

        // Subscription is updated and telemetry is collected in ProcessPendingStripeEvents when Stripe confirms the state change via webhook

        return Result.Success();
    }
}
