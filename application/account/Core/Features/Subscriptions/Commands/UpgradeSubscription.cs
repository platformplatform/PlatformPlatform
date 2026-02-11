using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.Account.Integrations.Stripe;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.Account.Features.Subscriptions.Commands;

[PublicAPI]
public sealed record UpgradeSubscriptionCommand(SubscriptionPlan NewPlan) : ICommand, IRequest<Result>;

public sealed class UpgradeSubscriptionHandler(
    ISubscriptionRepository subscriptionRepository,
    StripeClientFactory stripeClientFactory,
    IExecutionContext executionContext,
    ITelemetryEventsCollector events
) : IRequestHandler<UpgradeSubscriptionCommand, Result>
{
    public async Task<Result> Handle(UpgradeSubscriptionCommand command, CancellationToken cancellationToken)
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

        if (subscription.Plan >= command.NewPlan)
        {
            return Result.BadRequest($"Cannot upgrade from '{subscription.Plan}' to '{command.NewPlan}'. Target plan must be higher.");
        }

        var fromPlan = subscription.Plan;
        var stripeClient = stripeClientFactory.GetClient();
        var success = await stripeClient.UpgradeSubscriptionAsync(subscription.StripeSubscriptionId, command.NewPlan, cancellationToken);
        if (!success)
        {
            return Result.BadRequest("Failed to upgrade subscription in Stripe.");
        }

        events.CollectEvent(new SubscriptionUpgraded(subscription.Id, fromPlan, command.NewPlan));

        return Result.Success();
    }
}
