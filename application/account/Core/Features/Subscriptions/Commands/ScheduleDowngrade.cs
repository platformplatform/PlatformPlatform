using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.Account.Integrations.Stripe;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.Account.Features.Subscriptions.Commands;

[PublicAPI]
public sealed record ScheduleDowngradeCommand(SubscriptionPlan NewPlan) : ICommand, IRequest<Result>;

public sealed class ScheduleDowngradeHandler(
    ISubscriptionRepository subscriptionRepository,
    StripeClientFactory stripeClientFactory,
    IExecutionContext executionContext,
    ITelemetryEventsCollector events,
    ILogger<ScheduleDowngradeHandler> logger
) : IRequestHandler<ScheduleDowngradeCommand, Result>
{
    public async Task<Result> Handle(ScheduleDowngradeCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result.Forbidden("Only owners can manage subscriptions.");
        }

        var subscription = await subscriptionRepository.GetByTenantIdAsync(cancellationToken)
                           ?? throw new UnreachableException($"Subscription not found for tenant '{executionContext.TenantId}'.");

        if (subscription.StripeSubscriptionId is null)
        {
            logger.LogWarning("No Stripe subscription found for subscription '{SubscriptionId}'", subscription.Id);
            return Result.BadRequest("No active Stripe subscription found.");
        }

        if (subscription.Plan <= command.NewPlan)
        {
            return Result.BadRequest($"Cannot downgrade from '{subscription.Plan}' to '{command.NewPlan}'. Target plan must be lower.");
        }

        if (command.NewPlan == SubscriptionPlan.Basis)
        {
            return Result.BadRequest("Cannot downgrade to the Basis plan.");
        }

        var fromPlan = subscription.Plan;
        var stripeClient = stripeClientFactory.GetClient();
        var success = await stripeClient.ScheduleDowngradeAsync(subscription.StripeSubscriptionId, command.NewPlan, cancellationToken);
        if (!success)
        {
            return Result.BadRequest("Failed to schedule downgrade in Stripe.");
        }

        events.CollectEvent(new SubscriptionDowngradeScheduled(subscription.Id, fromPlan, command.NewPlan));

        return Result.Success();
    }
}
