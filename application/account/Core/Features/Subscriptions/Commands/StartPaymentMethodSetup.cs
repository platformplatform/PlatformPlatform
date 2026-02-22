using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.Account.Integrations.Stripe;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.Account.Features.Subscriptions.Commands;

[PublicAPI]
public sealed record StartPaymentMethodSetupCommand : ICommand, IRequest<Result<StartPaymentMethodSetupResponse>>;

[PublicAPI]
public sealed record StartPaymentMethodSetupResponse(string ClientSecret, string PublishableKey);

public sealed class StartPaymentMethodSetupHandler(
    ISubscriptionRepository subscriptionRepository,
    StripeClientFactory stripeClientFactory,
    IExecutionContext executionContext,
    ITelemetryEventsCollector events,
    ILogger<StartPaymentMethodSetupHandler> logger
) : IRequestHandler<StartPaymentMethodSetupCommand, Result<StartPaymentMethodSetupResponse>>
{
    public async Task<Result<StartPaymentMethodSetupResponse>> Handle(StartPaymentMethodSetupCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result<StartPaymentMethodSetupResponse>.Forbidden("Only owners can manage subscriptions.");
        }

        var subscription = await subscriptionRepository.GetCurrentAsync(cancellationToken);

        if (subscription.StripeCustomerId is null)
        {
            logger.LogWarning("No Stripe customer found for subscription '{SubscriptionId}'", subscription.Id);
            return Result<StartPaymentMethodSetupResponse>.BadRequest("No Stripe customer found. A subscription must be created first.");
        }

        var publishableKey = stripeClientFactory.GetPublishableKey();
        if (publishableKey is null)
        {
            logger.LogWarning("Stripe publishable key is not configured");
            return Result<StartPaymentMethodSetupResponse>.BadRequest("Stripe is not configured for payment method updates.");
        }

        var stripeClient = stripeClientFactory.GetClient();
        var clientSecret = await stripeClient.CreateSetupIntentAsync(subscription.StripeCustomerId, cancellationToken);
        if (clientSecret is null)
        {
            return Result<StartPaymentMethodSetupResponse>.BadRequest("Failed to create payment method setup.");
        }

        events.CollectEvent(new PaymentMethodSetupStarted(subscription.Id));

        return new StartPaymentMethodSetupResponse(clientSecret, publishableKey);
    }
}
