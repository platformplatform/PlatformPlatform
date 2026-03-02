using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.Account.Integrations.Stripe;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;

namespace PlatformPlatform.Account.Features.Subscriptions.Commands;

[PublicAPI]
public sealed record ReactivateSubscriptionCommand : ICommand, IRequest<Result<ReactivateSubscriptionResponse>>;

[PublicAPI]
public sealed record ReactivateSubscriptionResponse(string? ClientSecret, string? PublishableKey);

public sealed class ReactivateSubscriptionHandler(
    ISubscriptionRepository subscriptionRepository,
    StripeClientFactory stripeClientFactory,
    IExecutionContext executionContext,
    ILogger<ReactivateSubscriptionHandler> logger
) : IRequestHandler<ReactivateSubscriptionCommand, Result<ReactivateSubscriptionResponse>>
{
    public async Task<Result<ReactivateSubscriptionResponse>> Handle(ReactivateSubscriptionCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result<ReactivateSubscriptionResponse>.Forbidden("Only owners can manage subscriptions.");
        }

        var subscription = await subscriptionRepository.GetCurrentAsync(cancellationToken);

        if (!subscription.CancelAtPeriodEnd)
        {
            return Result<ReactivateSubscriptionResponse>.BadRequest("Subscription is not cancelled. Nothing to reactivate.");
        }

        if (subscription.StripeSubscriptionId is null)
        {
            logger.LogWarning("No Stripe subscription found for subscription '{SubscriptionId}'", subscription.Id);
            return Result<ReactivateSubscriptionResponse>.BadRequest("No active Stripe subscription found.");
        }

        var stripeClient = stripeClientFactory.GetClient();
        var reactivateSuccess = await stripeClient.ReactivateSubscriptionAsync(subscription.StripeSubscriptionId, cancellationToken);
        if (!reactivateSuccess)
        {
            return Result<ReactivateSubscriptionResponse>.BadRequest("Failed to reactivate subscription in Stripe.");
        }

        // Subscription is updated and telemetry is collected in ProcessPendingStripeEvents when Stripe confirms the state change via webhook

        return new ReactivateSubscriptionResponse(null, null);
    }
}
