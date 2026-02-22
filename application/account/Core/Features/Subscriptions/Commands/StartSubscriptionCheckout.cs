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
public sealed record StartSubscriptionCheckoutCommand(SubscriptionPlan Plan)
    : ICommand, IRequest<Result<StartSubscriptionCheckoutResponse>>;

[PublicAPI]
public sealed record StartSubscriptionCheckoutResponse(string? ClientSecret, string? PublishableKey, bool UsedExistingPaymentMethod);

public sealed class StartSubscriptionCheckoutValidator : AbstractValidator<StartSubscriptionCheckoutCommand>
{
    public StartSubscriptionCheckoutValidator()
    {
        RuleFor(x => x.Plan).NotEqual(SubscriptionPlan.Basis).WithMessage("Cannot subscribe to the Basis plan.");
    }
}

public sealed class StartSubscriptionCheckoutHandler(
    ISubscriptionRepository subscriptionRepository,
    StripeClientFactory stripeClientFactory,
    IExecutionContext executionContext,
    ITelemetryEventsCollector events,
    ILogger<StartSubscriptionCheckoutHandler> logger
) : IRequestHandler<StartSubscriptionCheckoutCommand, Result<StartSubscriptionCheckoutResponse>>
{
    public async Task<Result<StartSubscriptionCheckoutResponse>> Handle(StartSubscriptionCheckoutCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result<StartSubscriptionCheckoutResponse>.Forbidden("Only owners can manage subscriptions.");
        }

        var subscription = await subscriptionRepository.GetCurrentAsync(cancellationToken);

        if (subscription.StripeCustomerId is null)
        {
            return Result<StartSubscriptionCheckoutResponse>.BadRequest("Billing information must be saved before checkout.");
        }

        var publishableKey = stripeClientFactory.GetPublishableKey();
        if (publishableKey is null)
        {
            logger.LogWarning("Stripe publishable key is not configured");
            return Result<StartSubscriptionCheckoutResponse>.BadRequest("Stripe is not configured for checkout.");
        }

        var stripeClient = stripeClientFactory.GetClient();

        if (subscription.PaymentMethod is not null)
        {
            if (subscription.StripeSubscriptionId is not null)
            {
                return Result<StartSubscriptionCheckoutResponse>.BadRequest("A subscription already exists. Please complete any pending payment or use upgrade instead.");
            }

            if (subscription.BillingInfo is null)
            {
                return Result<StartSubscriptionCheckoutResponse>.BadRequest("Billing information must be saved before subscribing.");
            }

            var subscribeResult = await stripeClient.CreateSubscriptionWithSavedPaymentMethodAsync(subscription.StripeCustomerId, command.Plan, cancellationToken);
            if (subscribeResult is null)
            {
                return Result<StartSubscriptionCheckoutResponse>.BadRequest("Failed to create subscription in Stripe.");
            }

            events.CollectEvent(new SubscriptionCheckoutStarted(subscription.Id, command.Plan, true));

            var savedPublishableKey = subscribeResult.ClientSecret is not null ? publishableKey : null;
            return new StartSubscriptionCheckoutResponse(subscribeResult.ClientSecret, savedPublishableKey, true);
        }

        if (subscription.HasActiveStripeSubscription())
        {
            return Result<StartSubscriptionCheckoutResponse>.BadRequest("An active subscription already exists. Cannot create a new checkout session.");
        }

        var result = await stripeClient.CreateCheckoutSessionAsync(subscription.StripeCustomerId!, command.Plan, executionContext.UserInfo.Locale, cancellationToken);
        if (result is null)
        {
            return Result<StartSubscriptionCheckoutResponse>.BadRequest("Failed to create checkout session.");
        }

        events.CollectEvent(new SubscriptionCheckoutStarted(subscription.Id, command.Plan, false));

        return new StartSubscriptionCheckoutResponse(result.ClientSecret, publishableKey, false);
    }
}
