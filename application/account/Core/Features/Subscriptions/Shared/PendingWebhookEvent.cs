namespace Account.Features.Subscriptions.Shared;

/// <summary>
///     In-memory carrier for the webhook payload that
///     <see cref="Account.Features.Subscriptions.Commands.AcknowledgeStripeWebhookCommand" />
///     just acknowledged. Phase 2 of two-phase webhook processing receives this record directly from the
///     endpoint so it never has to re-read the just-persisted <c>stripe_events.payload</c> archive column.
///     Same shape as <see cref="Account.Integrations.Stripe.StripeReplayEvent" /> but exists at the
///     <c>Subscriptions</c> feature layer, decoupled from the Stripe integration boundary.
/// </summary>
public sealed record PendingWebhookEvent(
    string EventId,
    string EventType,
    DateTimeOffset StripeCreatedAt,
    string Payload,
    string ApiVersion
);
