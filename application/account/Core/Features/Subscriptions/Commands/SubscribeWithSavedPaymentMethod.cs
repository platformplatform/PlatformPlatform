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
public sealed record SubscribeWithSavedPaymentMethodCommand(SubscriptionPlan Plan) : ICommand, IRequest<Result<SubscribeWithSavedPaymentMethodResponse>>;

[PublicAPI]
public sealed record SubscribeWithSavedPaymentMethodResponse(string? ClientSecret, string? PublishableKey);

public sealed class SubscribeWithSavedPaymentMethodValidator : AbstractValidator<SubscribeWithSavedPaymentMethodCommand>
{
    public SubscribeWithSavedPaymentMethodValidator()
    {
        RuleFor(x => x.Plan).NotEqual(SubscriptionPlan.Basis).WithMessage("Cannot subscribe to the Basis plan.");
    }
}

public sealed class SubscribeWithSavedPaymentMethodHandler(
    ISubscriptionRepository subscriptionRepository,
    StripeClientFactory stripeClientFactory,
    IExecutionContext executionContext,
    ITelemetryEventsCollector events
) : IRequestHandler<SubscribeWithSavedPaymentMethodCommand, Result<SubscribeWithSavedPaymentMethodResponse>>
{
    public async Task<Result<SubscribeWithSavedPaymentMethodResponse>> Handle(SubscribeWithSavedPaymentMethodCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result<SubscribeWithSavedPaymentMethodResponse>.Forbidden("Only owners can manage subscriptions.");
        }

        var subscription = await subscriptionRepository.GetCurrentAsync(cancellationToken);

        if (subscription.StripeCustomerId is null)
        {
            return Result<SubscribeWithSavedPaymentMethodResponse>.BadRequest("Billing information must be saved before subscribing.");
        }

        if (subscription.StripeSubscriptionId is not null)
        {
            return Result<SubscribeWithSavedPaymentMethodResponse>.BadRequest("A subscription already exists. Please complete any pending payment or use upgrade instead.");
        }

        if (subscription.BillingInfo is null)
        {
            return Result<SubscribeWithSavedPaymentMethodResponse>.BadRequest("Billing information must be saved before subscribing.");
        }

        if (subscription.PaymentMethod is null)
        {
            return Result<SubscribeWithSavedPaymentMethodResponse>.BadRequest("A saved payment method is required to subscribe.");
        }

        var stripeClient = stripeClientFactory.GetClient();
        var subscribeResult = await stripeClient.CreateSubscriptionWithSavedPaymentMethodAsync(subscription.StripeCustomerId, command.Plan, cancellationToken);
        if (subscribeResult is null)
        {
            return Result<SubscribeWithSavedPaymentMethodResponse>.BadRequest("Failed to create subscription in Stripe.");
        }

        events.CollectEvent(new SubscriptionInitiated(subscription.Id, command.Plan, true));

        var publishableKey = subscribeResult.ClientSecret is not null ? stripeClientFactory.GetPublishableKey() : null;
        return new SubscribeWithSavedPaymentMethodResponse(subscribeResult.ClientSecret, publishableKey);
    }
}
