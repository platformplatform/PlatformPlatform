using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.Account.Integrations.Stripe;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.Account.Features.Subscriptions.Commands;

[PublicAPI]
public sealed record CreatePaymentMethodSetupCommand : ICommand, IRequest<Result<CreatePaymentMethodSetupResponse>>;

[PublicAPI]
public sealed record CreatePaymentMethodSetupResponse(string ClientSecret, string PublishableKey);

public sealed class CreatePaymentMethodSetupHandler(
    ISubscriptionRepository subscriptionRepository,
    StripeClientFactory stripeClientFactory,
    IExecutionContext executionContext,
    ITelemetryEventsCollector events,
    ILogger<CreatePaymentMethodSetupHandler> logger
) : IRequestHandler<CreatePaymentMethodSetupCommand, Result<CreatePaymentMethodSetupResponse>>
{
    public async Task<Result<CreatePaymentMethodSetupResponse>> Handle(CreatePaymentMethodSetupCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result<CreatePaymentMethodSetupResponse>.Forbidden("Only owners can manage subscriptions.");
        }

        var subscription = await subscriptionRepository.GetByTenantIdAsync(cancellationToken);
        if (subscription is null)
        {
            logger.LogWarning("Subscription not found for tenant '{TenantId}'", executionContext.TenantId);
            return Result<CreatePaymentMethodSetupResponse>.NotFound("Subscription not found for current tenant.");
        }

        if (subscription.StripeCustomerId is null)
        {
            logger.LogWarning("No Stripe customer found for subscription '{SubscriptionId}'", subscription.Id);
            return Result<CreatePaymentMethodSetupResponse>.BadRequest("No Stripe customer found. A subscription must be created first.");
        }

        var publishableKey = stripeClientFactory.GetPublishableKey();
        if (publishableKey is null)
        {
            logger.LogWarning("Stripe publishable key is not configured");
            return Result<CreatePaymentMethodSetupResponse>.BadRequest("Stripe is not configured for payment method updates.");
        }

        var stripeClient = stripeClientFactory.GetClient();
        var clientSecret = await stripeClient.CreateSetupIntentAsync(subscription.StripeCustomerId, cancellationToken);
        if (clientSecret is null)
        {
            return Result<CreatePaymentMethodSetupResponse>.BadRequest("Failed to create payment method setup.");
        }

        events.CollectEvent(new PaymentMethodSetupCreated(subscription.Id));

        return new CreatePaymentMethodSetupResponse(clientSecret, publishableKey);
    }
}
