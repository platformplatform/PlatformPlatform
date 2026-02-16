using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.Account.Features.Subscriptions.Shared;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.Account.Integrations.Stripe;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.Account.Features.Subscriptions.Commands;

[PublicAPI]
public sealed record UpgradeSubscriptionCommand(SubscriptionPlan NewPlan) : ICommand, IRequest<Result<UpgradeSubscriptionResponse>>;

[PublicAPI]
public sealed record UpgradeSubscriptionResponse(string? ClientSecret, string? PublishableKey);

public sealed class UpgradeSubscriptionHandler(
    ISubscriptionRepository subscriptionRepository,
    StripeClientFactory stripeClientFactory,
    IExecutionContext executionContext,
    ITelemetryEventsCollector events,
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

        var fromPlan = subscription.Plan;
        var stripeClient = stripeClientFactory.GetClient();
        var upgradeResult = await stripeClient.UpgradeSubscriptionAsync(subscription.StripeSubscriptionId, command.NewPlan, cancellationToken);
        if (upgradeResult is null)
        {
            return Result<UpgradeSubscriptionResponse>.BadRequest("Failed to upgrade subscription in Stripe.");
        }

        events.CollectEvent(new SubscriptionUpgraded(subscription.Id, fromPlan, command.NewPlan));

        var publishableKey = upgradeResult.ClientSecret is not null ? stripeClientFactory.GetPublishableKey() : null;
        return new UpgradeSubscriptionResponse(upgradeResult.ClientSecret, publishableKey);
    }
}
