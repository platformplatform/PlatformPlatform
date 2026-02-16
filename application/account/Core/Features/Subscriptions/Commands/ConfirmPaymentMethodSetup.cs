using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.Account.Integrations.Stripe;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.Account.Features.Subscriptions.Commands;

[PublicAPI]
public sealed record ConfirmPaymentMethodSetupCommand(string SetupIntentId) : ICommand, IRequest<Result>;

public sealed class ConfirmPaymentMethodSetupHandler(
    ISubscriptionRepository subscriptionRepository,
    StripeClientFactory stripeClientFactory,
    IExecutionContext executionContext,
    ITelemetryEventsCollector events,
    ILogger<ConfirmPaymentMethodSetupHandler> logger
) : IRequestHandler<ConfirmPaymentMethodSetupCommand, Result>
{
    public async Task<Result> Handle(ConfirmPaymentMethodSetupCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result.Forbidden("Only owners can manage subscriptions.");
        }

        var subscription = await subscriptionRepository.GetCurrentAsync(cancellationToken);

        if (subscription.StripeCustomerId is null)
        {
            logger.LogWarning("No Stripe customer found for subscription '{SubscriptionId}'", subscription.Id);
            return Result.BadRequest("No Stripe customer found. A subscription must be created first.");
        }

        if (subscription.StripeSubscriptionId is null)
        {
            logger.LogWarning("No Stripe subscription found for subscription '{SubscriptionId}'", subscription.Id);
            return Result.BadRequest("No active Stripe subscription found.");
        }

        var stripeClient = stripeClientFactory.GetClient();
        var paymentMethodId = await stripeClient.GetSetupIntentPaymentMethodAsync(command.SetupIntentId, cancellationToken);
        if (paymentMethodId is null)
        {
            return Result.BadRequest("Failed to retrieve payment method from setup intent.");
        }

        var success = await stripeClient.SetSubscriptionDefaultPaymentMethodAsync(subscription.StripeSubscriptionId, paymentMethodId, cancellationToken);
        if (!success)
        {
            return Result.BadRequest("Failed to update subscription payment method.");
        }

        var invoiceRetried = await stripeClient.RetryOpenInvoicePaymentAsync(subscription.StripeSubscriptionId, paymentMethodId, cancellationToken);
        if (invoiceRetried == true)
        {
            events.CollectEvent(new PendingInvoicePaymentRetried(subscription.Id));
        }

        events.CollectEvent(new PaymentMethodUpdated(subscription.Id));

        return Result.Success();
    }
}
