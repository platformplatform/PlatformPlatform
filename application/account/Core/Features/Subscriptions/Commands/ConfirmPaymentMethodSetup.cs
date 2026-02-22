using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.Account.Integrations.Stripe;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;

namespace PlatformPlatform.Account.Features.Subscriptions.Commands;

[PublicAPI]
public sealed record ConfirmPaymentMethodSetupCommand(string SetupIntentId) : ICommand, IRequest<Result<ConfirmPaymentMethodSetupResponse>>;

[PublicAPI]
public sealed record ConfirmPaymentMethodSetupResponse(bool HasOpenInvoice, decimal? OpenInvoiceAmount, string? OpenInvoiceCurrency);

public sealed class ConfirmPaymentMethodSetupHandler(
    ISubscriptionRepository subscriptionRepository,
    StripeClientFactory stripeClientFactory,
    IExecutionContext executionContext,
    ILogger<ConfirmPaymentMethodSetupHandler> logger
) : IRequestHandler<ConfirmPaymentMethodSetupCommand, Result<ConfirmPaymentMethodSetupResponse>>
{
    public async Task<Result<ConfirmPaymentMethodSetupResponse>> Handle(ConfirmPaymentMethodSetupCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result<ConfirmPaymentMethodSetupResponse>.Forbidden("Only owners can manage subscriptions.");
        }

        var subscription = await subscriptionRepository.GetCurrentAsync(cancellationToken);

        if (subscription.StripeCustomerId is null)
        {
            logger.LogWarning("No Stripe customer found for subscription '{SubscriptionId}'", subscription.Id);
            return Result<ConfirmPaymentMethodSetupResponse>.BadRequest("No Stripe customer found. A subscription must be created first.");
        }

        var stripeClient = stripeClientFactory.GetClient();
        var paymentMethodId = await stripeClient.GetSetupIntentPaymentMethodAsync(command.SetupIntentId, cancellationToken);
        if (paymentMethodId is null)
        {
            return Result<ConfirmPaymentMethodSetupResponse>.BadRequest("Failed to retrieve payment method from setup intent.");
        }

        OpenInvoiceResult? openInvoice = null;
        if (subscription.StripeSubscriptionId is not null)
        {
            var success = await stripeClient.SetSubscriptionDefaultPaymentMethodAsync(subscription.StripeSubscriptionId, paymentMethodId, cancellationToken);
            if (!success)
            {
                return Result<ConfirmPaymentMethodSetupResponse>.BadRequest("Failed to update subscription payment method.");
            }

            openInvoice = await stripeClient.GetOpenInvoiceAsync(subscription.StripeSubscriptionId, cancellationToken);
        }
        else
        {
            var success = await stripeClient.SetCustomerDefaultPaymentMethodAsync(subscription.StripeCustomerId, paymentMethodId, cancellationToken);
            if (!success)
            {
                return Result<ConfirmPaymentMethodSetupResponse>.BadRequest("Failed to update customer payment method.");
            }
        }

        // Subscription is updated and telemetry is collected in ProcessPendingStripeEvents when Stripe confirms the state change via webhook

        return new ConfirmPaymentMethodSetupResponse(openInvoice is not null, openInvoice?.AmountDue, openInvoice?.Currency);
    }
}
