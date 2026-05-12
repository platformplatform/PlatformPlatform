using Account.Features.Subscriptions.Domain;
using Account.Features.Subscriptions.Shared;
using Account.Integrations.Stripe;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Telemetry;

namespace Account.Features.Subscriptions.Commands;

/// <summary>
///     Phase 1 of two-phase webhook processing. Validates the Stripe signature, stores the event as
///     pending, and returns the resolved customer id alongside the just-acked event so the API can
///     trigger phase 2 processing without re-reading the durable <c>stripe_events.payload</c> archive.
/// </summary>
[PublicAPI]
public sealed record AcknowledgeStripeWebhookCommand(string Payload, string SignatureHeader) : ICommand, IRequest<Result<AcknowledgeStripeWebhookResult?>>;

/// <summary>
///     Carries the resolved customer id and (when the webhook is new and supported) the in-memory
///     payload for phase 2 to process. <see cref="JustAcknowledgedEvent" /> is null when the webhook is a
///     duplicate (the existing row's payload hash matched), when the customer is unknown (the row was
///     stored but marked Ignored), or when the event type is not subscription-relevant for this tenant.
/// </summary>
[PublicAPI]
public sealed record AcknowledgeStripeWebhookResult(StripeCustomerId StripeCustomerId, PendingWebhookEvent? JustAcknowledgedEvent);

public sealed class AcknowledgeStripeWebhookHandler(
    IStripeEventRepository stripeEventRepository,
    StripeClientFactory stripeClientFactory,
    ITelemetryEventsCollector events,
    TimeProvider timeProvider,
    ILogger<AcknowledgeStripeWebhookHandler> logger
) : IRequestHandler<AcknowledgeStripeWebhookCommand, Result<AcknowledgeStripeWebhookResult?>>
{
    public async Task<Result<AcknowledgeStripeWebhookResult?>> Handle(AcknowledgeStripeWebhookCommand command, CancellationToken cancellationToken)
    {
        var stripeClient = stripeClientFactory.GetClient();
        var webhookEvent = stripeClient.VerifyWebhookSignature(command.Payload, command.SignatureHeader);
        if (webhookEvent is null)
        {
            return Result<AcknowledgeStripeWebhookResult?>.BadRequest("Invalid webhook signature.");
        }

        var payloadHash = StripeEventPayloadHasher.Hash(command.Payload);

        // Idempotency: Stripe redelivers webhooks on transient errors (network, our 5xx, etc.). Same event
        // id arriving twice with the same payload is a no-op. Same id with a *different* payload is a
        // forensic anomaly: the existing row is preserved unchanged and a divergence telemetry event is
        // emitted so the drift banner can surface it. We never overwrite stripe_events rows.
        var existing = await stripeEventRepository.GetByIdAsync(StripeEventId.NewId(webhookEvent.EventId), cancellationToken);
        if (existing is not null)
        {
            // Short-circuit when the existing row has no payload hash recorded — legacy rows from before
            // the payload_hash column existed have NULL there, and stripe_events is INSERT-only so we never
            // re-hash the stored payload to backfill. Treat null as "no prior hash to compare against",
            // not as divergence.
            if (existing.PayloadHash is not null && existing.PayloadHash != payloadHash)
            {
                logger.LogWarning(
                    "Stripe event {EventId} arrived twice with different payloads (existing hash {ExistingHash} vs new {NewHash}); existing row preserved",
                    webhookEvent.EventId, existing.PayloadHash, payloadHash
                );
                events.CollectEvent(new StripeEventPayloadMismatch(webhookEvent.EventId, webhookEvent.EventType, existing.PayloadHash, payloadHash));
            }

            // Redeliveries return the customer id but no JustAcknowledgedEvent: phase 2 already processed the
            // original arrival (or is about to via a concurrent request); replaying the same payload
            // would just produce a no-op since billing_events is idempotent on stripe_event_id.
            return webhookEvent.CustomerId is null
                ? Result<AcknowledgeStripeWebhookResult?>.Success(null)
                : new AcknowledgeStripeWebhookResult(webhookEvent.CustomerId, null);
        }

        var now = timeProvider.GetUtcNow();
        var customerId = webhookEvent.CustomerId;

        var stripeEvent = StripeEvent.Create(webhookEvent.EventId, webhookEvent.EventType, customerId, command.Payload, webhookEvent.ApiVersion, payloadHash, webhookEvent.Created);

        if (customerId is null)
        {
            stripeEvent.MarkIgnored(now);
        }

        await stripeEventRepository.AddAsync(stripeEvent, cancellationToken);

        if (customerId is null) return Result<AcknowledgeStripeWebhookResult?>.Success(null);

        var justAcknowledgedEvent = new PendingWebhookEvent(
            webhookEvent.EventId,
            webhookEvent.EventType,
            webhookEvent.Created,
            command.Payload,
            webhookEvent.ApiVersion
        );
        return new AcknowledgeStripeWebhookResult(customerId, justAcknowledgedEvent);
    }
}
