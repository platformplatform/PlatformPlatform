using JetBrains.Annotations;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.Account.Integrations.Stripe;
using PlatformPlatform.SharedKernel.Cqrs;

namespace PlatformPlatform.Account.Features.Subscriptions.Commands;

/// <summary>
///     Phase 1 of two-phase webhook processing. Validates the Stripe signature, stores the event
///     as pending, and returns the customer ID so the API can trigger phase 2 processing.
/// </summary>
[PublicAPI]
public sealed record AcknowledgeStripeWebhookCommand(string Payload, string SignatureHeader) : ICommand, IRequest<Result<StripeCustomerId?>>;

public sealed class AcknowledgeStripeWebhookHandler(
    IStripeEventRepository stripeEventRepository,
    StripeClientFactory stripeClientFactory,
    TimeProvider timeProvider
) : IRequestHandler<AcknowledgeStripeWebhookCommand, Result<StripeCustomerId?>>
{
    public async Task<Result<StripeCustomerId?>> Handle(AcknowledgeStripeWebhookCommand command, CancellationToken cancellationToken)
    {
        var stripeClient = stripeClientFactory.GetClient();
        var webhookEvent = stripeClient.VerifyWebhookSignature(command.Payload, command.SignatureHeader);
        if (webhookEvent is null)
        {
            return Result<StripeCustomerId?>.BadRequest("Invalid webhook signature.");
        }

        if (await stripeEventRepository.ExistsAsync(webhookEvent.EventId, cancellationToken))
        {
            return Result<StripeCustomerId?>.Success(webhookEvent.CustomerId);
        }

        var now = timeProvider.GetUtcNow();
        var customerId = webhookEvent.CustomerId;

        var stripeEvent = StripeEvent.Create(webhookEvent.EventId, webhookEvent.EventType, customerId, command.Payload);

        if (customerId is null)
        {
            stripeEvent.MarkIgnored(now);
        }

        await stripeEventRepository.AddAsync(stripeEvent, cancellationToken);

        return customerId;
    }
}
