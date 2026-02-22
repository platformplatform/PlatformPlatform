using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.Account.Integrations.Stripe;
using PlatformPlatform.SharedKernel.Cqrs;
using PlatformPlatform.SharedKernel.ExecutionContext;
using PlatformPlatform.SharedKernel.Telemetry;

namespace PlatformPlatform.Account.Features.Subscriptions.Commands;

[PublicAPI]
public sealed record RetryPendingInvoicePaymentCommand : ICommand, IRequest<Result<RetryPendingInvoicePaymentResponse>>;

[PublicAPI]
public sealed record RetryPendingInvoicePaymentResponse(bool Paid, string? ClientSecret, string? PublishableKey);

public sealed class RetryPendingInvoicePaymentHandler(
    ISubscriptionRepository subscriptionRepository,
    StripeClientFactory stripeClientFactory,
    IExecutionContext executionContext,
    ITelemetryEventsCollector events,
    ILogger<RetryPendingInvoicePaymentHandler> logger
) : IRequestHandler<RetryPendingInvoicePaymentCommand, Result<RetryPendingInvoicePaymentResponse>>
{
    public async Task<Result<RetryPendingInvoicePaymentResponse>> Handle(RetryPendingInvoicePaymentCommand command, CancellationToken cancellationToken)
    {
        if (executionContext.UserInfo.Role != nameof(UserRole.Owner))
        {
            return Result<RetryPendingInvoicePaymentResponse>.Forbidden("Only owners can manage subscriptions.");
        }

        var subscription = await subscriptionRepository.GetCurrentAsync(cancellationToken);

        if (subscription.StripeSubscriptionId is null)
        {
            logger.LogWarning("No Stripe subscription found for subscription '{SubscriptionId}'", subscription.Id);
            return Result<RetryPendingInvoicePaymentResponse>.BadRequest("No active Stripe subscription found.");
        }

        var stripeClient = stripeClientFactory.GetClient();

        var openInvoice = await stripeClient.GetOpenInvoiceAsync(subscription.StripeSubscriptionId, cancellationToken);
        if (openInvoice is null)
        {
            return Result<RetryPendingInvoicePaymentResponse>.BadRequest("No pending invoice found for this subscription.");
        }

        var invoiceRetryResult = await stripeClient.RetryOpenInvoicePaymentAsync(subscription.StripeSubscriptionId, null, cancellationToken);
        if (invoiceRetryResult is null)
        {
            return Result<RetryPendingInvoicePaymentResponse>.BadRequest("Failed to retry invoice payment.");
        }

        if (invoiceRetryResult is { Paid: false, ClientSecret: null, ErrorMessage: not null })
        {
            return Result<RetryPendingInvoicePaymentResponse>.BadRequest(invoiceRetryResult.ErrorMessage);
        }

        if (invoiceRetryResult.Paid)
        {
            events.CollectEvent(new PendingInvoicePaymentRetried(subscription.Id));
        }

        var publishableKey = invoiceRetryResult.ClientSecret is not null ? stripeClientFactory.GetPublishableKey() : null;
        return new RetryPendingInvoicePaymentResponse(invoiceRetryResult.Paid, invoiceRetryResult.ClientSecret, publishableKey);
    }
}
