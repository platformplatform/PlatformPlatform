using System.Text;
using Account.Database;
using Account.Features.Subscriptions.Domain;
using Account.Features.Subscriptions.Shared;
using Account.Integrations.Stripe;
using FluentAssertions;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Subscriptions;

/// <summary>
///     Hard rule: rows in <c>stripe_events</c> are append-only and never overwritten. A redelivered
///     Stripe event arriving with a different payload is treated as a forensic anomaly — the existing
///     row is preserved unchanged and a <c>StripeEventPayloadMismatch</c> telemetry event is emitted
///     so the drift banner can surface it.
/// </summary>
public sealed class StripeEventPayloadDivergenceTests : EndpointBaseTest<AccountDbContext>
{
    private const string WebhookUrl = "/api/account/subscriptions/stripe-webhook";

    [Fact]
    public async Task AcknowledgeStripeWebhook_WhenSameEventArrivesWithDifferentPayload_ShouldNotOverwriteExistingRow()
    {
        // Arrange — pre-insert a Stripe event with a known payload hash
        var eventId = $"{MockStripeClient.MockWebhookEventId}_divergence";
        var originalPayload = "customer:cus_original_payload";
        var originalHash = StripeEventPayloadHasher.Hash(originalPayload);

        Connection.Insert("stripe_events", [
                ("tenant_id", null),
                ("id", eventId),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("event_type", "customer.subscription.updated"),
                ("status", nameof(StripeEventStatus.Processed)),
                ("processed_at", TimeProvider.GetUtcNow()),
                ("stripe_customer_id", MockStripeClient.MockCustomerId),
                ("stripe_subscription_id", null),
                ("payload", originalPayload),
                ("error", null),
                ("api_version", MockStripeClient.MockApiVersion),
                ("payload_hash", originalHash),
                ("recovered_at", null),
                ("recovery_source", null)
            ]
        );

        // Act — same event_id arrives again with a different body (divergent payload)
        var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent("customer:cus_DIFFERENT_payload", Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Stripe-Signature", $"event_type:customer.subscription.updated,event_id:{eventId}");
        var response = await AnonymousHttpClient.SendAsync(request);

        // Assert — the existing row's payload and hash must be unchanged
        response.EnsureSuccessStatusCode();

        var preservedPayload = Connection.ExecuteScalar<string>("SELECT payload FROM stripe_events WHERE id = @id", [new { id = eventId }]);
        preservedPayload.Should().Be(originalPayload, "the existing row must not be overwritten when a different payload arrives for the same id");

        var preservedHash = Connection.ExecuteScalar<string>("SELECT payload_hash FROM stripe_events WHERE id = @id", [new { id = eventId }]);
        preservedHash.Should().Be(originalHash);

        var rowCount = Connection.ExecuteScalar<long>("SELECT COUNT(*) FROM stripe_events WHERE id = @id", [new { id = eventId }]);
        rowCount.Should().Be(1, "no duplicate row should be inserted for the divergent payload");
    }
}
