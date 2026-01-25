using FluentValidation;
using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.Account.Integrations.Stripe;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.Account.Features.Subscriptions.Commands;

[PublicAPI]
public sealed record CheckoutSuccessCommand(string SessionId) : ICommand, IRequest<Result>;

public sealed class CheckoutSuccessValidator : AbstractValidator<CheckoutSuccessCommand>
{
    public CheckoutSuccessValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty().WithMessage("Session ID is required.");
    }
}

public sealed class CheckoutSuccessHandler(
    ISubscriptionRepository subscriptionRepository,
    StripeClientFactory stripeClientFactory,
    IExecutionContext executionContext,
    ITelemetryEventsCollector events
) : IRequestHandler<CheckoutSuccessCommand, Result>
{
    public async Task<Result> Handle(CheckoutSuccessCommand command, CancellationToken cancellationToken)
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

        if (subscription.StripeCustomerId is null)
        {
            return Result.BadRequest("No Stripe customer found for this subscription.");
        }

        var stripeClient = stripeClientFactory.GetClient();
        var syncResult = await stripeClient.SyncSubscriptionStateAsync(subscription.StripeCustomerId, cancellationToken);
        if (syncResult is null)
        {
            return Result.BadRequest("No subscription found in Stripe after checkout.");
        }

        subscription.SyncFromStripe(
            syncResult.Plan,
            syncResult.ScheduledPlan,
            syncResult.StripeSubscriptionId,
            syncResult.CurrentPeriodEnd,
            syncResult.CancelAtPeriodEnd,
            [.. syncResult.PaymentTransactions]
        );
        subscriptionRepository.Update(subscription);

        events.CollectEvent(new SubscriptionCreated(subscription.Id, syncResult.Plan));

        return Result.Success();
    }
}
