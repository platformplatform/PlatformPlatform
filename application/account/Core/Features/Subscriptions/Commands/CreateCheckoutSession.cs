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
public sealed record CreateCheckoutSessionCommand(SubscriptionPlan Plan, string ReturnUrl)
    : ICommand, IRequest<Result<CreateCheckoutSessionResponse>>;

[PublicAPI]
public sealed record CreateCheckoutSessionResponse(string ClientSecret, string PublishableKey);

public sealed class CreateCheckoutSessionValidator : AbstractValidator<CreateCheckoutSessionCommand>
{
    public CreateCheckoutSessionValidator()
    {
        RuleFor(x => x.Plan).NotEqual(SubscriptionPlan.Basis).WithMessage("Cannot create a checkout session for the Basis plan.");
        RuleFor(x => x.ReturnUrl).NotEmpty().WithMessage("Return URL is required.");
    }
}

public sealed class CreateCheckoutSessionHandler(
    ISubscriptionRepository subscriptionRepository,
    StripeClientFactory stripeClientFactory,
    IExecutionContext executionContext,
    ITelemetryEventsCollector events,
    ILogger<CreateCheckoutSessionHandler> logger
) : IRequestHandler<CreateCheckoutSessionCommand, Result<CreateCheckoutSessionResponse>>
{
    public async Task<Result<CreateCheckoutSessionResponse>> Handle(CreateCheckoutSessionCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result<CreateCheckoutSessionResponse>.Forbidden("Only owners can manage subscriptions.");
        }

        if (executionContext.UserInfo.Email is null)
        {
            return Result<CreateCheckoutSessionResponse>.BadRequest("User email is required to create a checkout session.");
        }

        var subscription = await subscriptionRepository.GetByTenantIdAsync(cancellationToken);
        if (subscription is null)
        {
            logger.LogWarning("Subscription not found for tenant '{TenantId}'", executionContext.TenantId);
            return Result<CreateCheckoutSessionResponse>.NotFound("Subscription not found for current tenant.");
        }

        if (subscription.HasActiveStripeSubscription())
        {
            return Result<CreateCheckoutSessionResponse>.BadRequest("An active subscription already exists. Cannot create a new checkout session.");
        }

        var publishableKey = stripeClientFactory.GetPublishableKey();
        if (publishableKey is null)
        {
            logger.LogWarning("Stripe publishable key is not configured");
            return Result<CreateCheckoutSessionResponse>.BadRequest("Stripe is not configured for checkout.");
        }

        var stripeClient = stripeClientFactory.GetClient();

        if (subscription.StripeCustomerId is null)
        {
            var billingName = subscription.BillingInfo?.Name;
            if (billingName is null)
            {
                return Result<CreateCheckoutSessionResponse>.BadRequest("Billing information is required before checkout.");
            }

            var customerId = await stripeClient.CreateCustomerAsync(billingName, executionContext.UserInfo.Email, subscription.TenantId.Value, cancellationToken);
            if (customerId is null)
            {
                return Result<CreateCheckoutSessionResponse>.BadRequest("Failed to create Stripe customer.");
            }

            subscription.SetStripeCustomerId(customerId);
            subscriptionRepository.Update(subscription);
        }

        var locale = executionContext.UserInfo.Locale?[..2] ?? "en";
        var result = await stripeClient.CreateCheckoutSessionAsync(subscription.StripeCustomerId!, command.Plan, command.ReturnUrl, locale, cancellationToken);
        if (result is null)
        {
            return Result<CreateCheckoutSessionResponse>.BadRequest("Failed to create checkout session.");
        }

        events.CollectEvent(new CheckoutSessionCreated(subscription.Id, command.Plan));

        return new CreateCheckoutSessionResponse(result.ClientSecret, publishableKey);
    }
}
