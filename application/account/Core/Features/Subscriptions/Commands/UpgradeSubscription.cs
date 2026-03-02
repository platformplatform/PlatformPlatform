using Account.Features.Subscriptions.Domain;
using Account.Features.Subscriptions.Shared;
using Account.Features.Users.Domain;
using Account.Integrations.Stripe;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.ExecutionContext;

namespace Account.Features.Subscriptions.Commands;

[PublicAPI]
public sealed record UpgradeSubscriptionCommand(SubscriptionPlan NewPlan) : ICommand, IRequest<Result<UpgradeSubscriptionResponse>>;

[PublicAPI]
public sealed record UpgradeSubscriptionResponse(string? ClientSecret, string? PublishableKey);

public sealed class UpgradeSubscriptionHandler(
    ISubscriptionRepository subscriptionRepository,
    StripeClientFactory stripeClientFactory,
    IExecutionContext executionContext,
    ILogger<UpgradeSubscriptionHandler> logger
) : IRequestHandler<UpgradeSubscriptionCommand, Result<UpgradeSubscriptionResponse>>
{
    public async Task<Result<UpgradeSubscriptionResponse>> Handle(UpgradeSubscriptionCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result<UpgradeSubscriptionResponse>.Forbidden("Only owners can manage subscriptions.");
        }

        var subscription = await subscriptionRepository.GetCurrentAsync(cancellationToken);

        if (subscription.StripeSubscriptionId is null)
        {
            logger.LogWarning("No Stripe subscription found for subscription '{SubscriptionId}'", subscription.Id);
            return Result<UpgradeSubscriptionResponse>.BadRequest("No active Stripe subscription found.");
        }

        if (!command.NewPlan.IsUpgradeFrom(subscription.Plan))
        {
            return Result<UpgradeSubscriptionResponse>.BadRequest($"Cannot upgrade from '{subscription.Plan}' to '{command.NewPlan}'. Target plan must be higher.");
        }

        var stripeClient = stripeClientFactory.GetClient();
        var upgradeResult = await stripeClient.UpgradeSubscriptionAsync(subscription.StripeSubscriptionId, command.NewPlan, cancellationToken);
        if (upgradeResult is null)
        {
            return Result<UpgradeSubscriptionResponse>.BadRequest("Failed to upgrade subscription in Stripe.");
        }

        if (upgradeResult.ErrorMessage is not null)
        {
            return Result<UpgradeSubscriptionResponse>.BadRequest(upgradeResult.ErrorMessage);
        }

        // Subscription is updated and telemetry is collected in ProcessPendingStripeEvents when Stripe confirms the state change via webhook

        var publishableKey = upgradeResult.ClientSecret is not null ? stripeClientFactory.GetPublishableKey() : null;
        return new UpgradeSubscriptionResponse(upgradeResult.ClientSecret, publishableKey);
    }
}
