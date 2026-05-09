using Account.Features.Subscriptions.Domain;
using Account.Features.Subscriptions.Shared;
using Account.Integrations.Stripe;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Telemetry;

namespace Account.Features.Subscriptions.Commands;

/// <summary>
///     Phase 1 of two-phase webhook processing. Validates the Stripe signature, stores the event
///     as pending, and returns the customer ID so the API can trigger phase 2 processing.
/// </summary>
[PublicAPI]
public sealed record AcknowledgeStripeWebhookCommand(string Payload, string SignatureHeader) : ICommand, IRequest<Result<StripeCustomerId?>>;

public sealed class AcknowledgeStripeWebhookHandler(
    IStripeEventRepository stripeEventRepository,
    StripeClientFactory stripeClientFactory,
    ITelemetryEventsCollector events,
    TimeProvider timeProvider,
    ILogger<AcknowledgeStripeWebhookHandler> logger
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

        var payloadHash = StripeEventPayloadHasher.Hash(command.Payload);

        // Idempotency: Stripe redelivers webhooks on transient errors (network, our 5xx, etc.). Same event
        // id arriving twice with the same payload is a no-op. Same id with a *different* payload is a
        // forensic anomaly: the existing row is preserved unchanged and a divergence telemetry event is
        // emitted so the drift banner can surface it. We never overwrite stripe_events rows.
        var existing = await stripeEventRepository.GetByIdAsync(StripeEventId.NewId(webhookEvent.EventId), cancellationToken);
        if (existing is not null)
        {
            if (existing.PayloadHash is not null && existing.PayloadHash != payloadHash)
            {
                logger.LogWarning(
                    "Stripe event {EventId} arrived twice with different payloads (existing hash {ExistingHash} vs new {NewHash}); existing row preserved",
                    webhookEvent.EventId, existing.PayloadHash, payloadHash
                );
                events.CollectEvent(new StripeEventPayloadMismatch(webhookEvent.EventId, webhookEvent.EventType, existing.PayloadHash, payloadHash));
            }

            return Result<StripeCustomerId?>.Success(webhookEvent.CustomerId);
        }

        var now = timeProvider.GetUtcNow();
        var customerId = webhookEvent.CustomerId;

        var stripeEvent = StripeEvent.Create(webhookEvent.EventId, webhookEvent.EventType, customerId, command.Payload, webhookEvent.ApiVersion, payloadHash);

        if (customerId is null)
        {
            stripeEvent.MarkIgnored(now);
        }

        await stripeEventRepository.AddAsync(stripeEvent, cancellationToken);

        return customerId;
    }
}
