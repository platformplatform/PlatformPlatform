using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using Account.Features.Subscriptions.Domain;
using Account.Features.Subscriptions.Shared;
using Account.Features.Tenants.BackOffice.Commands;
using Account.Integrations.OAuth;
using Account.Integrations.Stripe;
using FluentAssertions;
using SharedKernel.Authentication.MockEasyAuth;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.BackOffice;

public sealed class ReplayArchivedTenantStripeEventsTests : BackOfficeEndpointBaseTest
{
    [Fact]
    public async Task ExecuteAsync_ReplaysArchivedEventsIntoBillingEvents()
    {
        // Arrange
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("stripe_customer_id", MockStripeClient.MockCustomerId)
            ]
        );

        var archivedOccurredAt = DateTimeOffset.UtcNow.AddDays(-45);
        InsertArchivedSubscriptionCreatedEvent(archivedOccurredAt);

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "admin");
        using var client = CreateBackOfficeClientForIdentity(identity);
        client.DefaultRequestHeaders.Add("Cookie", $"{OAuthProviderFactory.UseMockProviderCookieName}=true");

        // Act
        var response = await client.PostAsync($"/api/back-office/tenants/{DatabaseSeeder.Tenant1.Id}/replay-archived-stripe-events", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ReplayArchivedTenantStripeEventsResponse>();
        payload.Should().NotBeNull();
        payload.BillingEventsAppended.Should().Be(1);

        var billingEventCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM billing_events WHERE tenant_id = @tenantId AND event_type = @eventType",
            [new { tenantId = DatabaseSeeder.Tenant1.Id.Value, eventType = nameof(BillingEventType.SubscriptionCreated) }]
        );
        billingEventCount.Should().Be(1);

        TelemetryEventsCollectorSpy.CollectedEvents.Should().ContainSingle(e => e.GetType().Name == "TenantStripeArchiveReplayed");
    }

    [Fact]
    public async Task ExecuteAsync_SeedsReplayStateFromPersistedHistory()
    {
        // Arrange
        Connection.Update("subscriptions", "tenant_id", DatabaseSeeder.Tenant1.Id.Value, [
                ("stripe_customer_id", MockStripeClient.MockCustomerId),
                ("current_price_currency", "DKK")
            ]
        );

        var seededOccurredAt = DateTimeOffset.UtcNow.AddDays(-90);
        var subscriptionId = Connection.ExecuteScalar<string>(
            "SELECT id FROM subscriptions WHERE tenant_id = @tenantId",
            [new { tenantId = DatabaseSeeder.Tenant1.Id.Value }]
        );
        Connection.Insert("billing_events", [
                ("tenant_id", DatabaseSeeder.Tenant1.Id.Value),
                ("id", BillingEventId.NewId().Value),
                ("subscription_id", subscriptionId),
                ("created_at", seededOccurredAt),
                ("modified_at", null),
                ("stripe_event_id", $"evt_seed_{Guid.NewGuid():N}"),
                ("event_type", nameof(BillingEventType.SubscriptionCreated)),
                ("from_plan", null),
                ("to_plan", nameof(SubscriptionPlan.Standard)),
                ("previous_amount", 0m),
                ("new_amount", 299m),
                ("amount_delta", 299m),
                ("committed_mrr", 299m),
                ("currency", "DKK"),
                ("occurred_at", seededOccurredAt),
                ("cancellation_reason", null),
                ("suspension_reason", null)
            ]
        );

        // Archive a cancel-at-period-end toggle: the only "diff" Stripe carries is cancel_at_period_end: true.
        // Without ReplayState seeding from history this row would emit previousAmount=0, amountDelta=0,
        // committedMrr=0. With seeding it must derive previousAmount=299 / amountDelta=-299 / committedMrr=0
        // from the latest persisted row.
        var archivedOccurredAt = DateTimeOffset.UtcNow.AddDays(-45);
        var cancelEventId = $"evt_archive_cancel_{Guid.NewGuid():N}";
        var cancelPayload = """{"data":{"object":{"cancel_at_period_end":true,"currency":"dkk","items":{"data":[{"price":{"id":"price_mock_standard","unit_amount":29900,"currency":"dkk"}}]}},"previous_attributes":{"cancel_at_period_end":false}}}""";
        Connection.Insert("stripe_events", [
                ("tenant_id", DatabaseSeeder.Tenant1.Id.Value),
                ("id", cancelEventId),
                ("created_at", archivedOccurredAt),
                ("modified_at", null),
                ("event_type", "customer.subscription.updated"),
                ("status", nameof(StripeEventStatus.Processed)),
                ("processed_at", archivedOccurredAt),
                ("stripe_customer_id", MockStripeClient.MockCustomerId),
                ("stripe_subscription_id", MockStripeClient.MockSubscriptionId),
                ("payload", cancelPayload),
                ("error", null),
                ("api_version", MockStripeClient.MockApiVersion),
                ("payload_hash", StripeEventPayloadHasher.Hash(cancelPayload)),
                ("stripe_created_at", archivedOccurredAt)
            ]
        );

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "admin");
        using var client = CreateBackOfficeClientForIdentity(identity);
        client.DefaultRequestHeaders.Add("Cookie", $"{OAuthProviderFactory.UseMockProviderCookieName}=true");

        // Act
        var response = await client.PostAsync($"/api/back-office/tenants/{DatabaseSeeder.Tenant1.Id}/replay-archived-stripe-events", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<ReplayArchivedTenantStripeEventsResponse>();
        payload.Should().NotBeNull();
        payload.BillingEventsAppended.Should().Be(1);

        var eventType = Connection.ExecuteScalar<string>(
            "SELECT event_type FROM billing_events WHERE stripe_event_id = @stripeEventId",
            [new { stripeEventId = cancelEventId }]
        );
        eventType.Should().Be(nameof(BillingEventType.SubscriptionCancelled), "cancel-toggle archive event must classify as SubscriptionCancelled");

        var previousAmount = ReadDecimalColumn("previous_amount", cancelEventId);
        var amountDelta = ReadDecimalColumn("amount_delta", cancelEventId);
        var committedMrr = ReadDecimalColumn("committed_mrr", cancelEventId);
        previousAmount.Should().Be(299m, "previousAmount must derive from the seeded SubscriptionCreated PlanPrice");
        amountDelta.Should().Be(-299m, "amountDelta must reflect MRR loss equal to the seeded committed_mrr");
        committedMrr.Should().Be(0m, "committedMrr after cancellation must drop to zero");
    }

    private decimal? ReadDecimalColumn(string columnName, string stripeEventId)
    {
        // EF Core maps decimal to TEXT in SQLite to preserve precision; the test helper's direct cast to
        // decimal therefore fails — read the raw string and parse with InvariantCulture.
        var raw = Connection.ExecuteScalar<string?>(
            $"SELECT {columnName} FROM billing_events WHERE stripe_event_id = @stripeEventId",
            [new { stripeEventId }]
        );
        return raw is null ? null : decimal.Parse(raw, CultureInfo.InvariantCulture);
    }

    private void InsertArchivedSubscriptionCreatedEvent(DateTimeOffset archivedOccurredAt)
    {
        var archivedEventId = $"evt_archive_{Guid.NewGuid():N}";
        var archivedPayload = """{"data":{"object":{"currency":"dkk","items":{"data":[{"price":{"id":"price_mock_standard","unit_amount":2999,"currency":"dkk"}}]}}}}""";
        Connection.Insert("stripe_events", [
                ("tenant_id", DatabaseSeeder.Tenant1.Id.Value),
                ("id", archivedEventId),
                ("created_at", archivedOccurredAt),
                ("modified_at", null),
                ("event_type", "customer.subscription.created"),
                ("status", nameof(StripeEventStatus.Processed)),
                ("processed_at", archivedOccurredAt),
                ("stripe_customer_id", MockStripeClient.MockCustomerId),
                ("stripe_subscription_id", MockStripeClient.MockSubscriptionId),
                ("payload", archivedPayload),
                ("error", null),
                ("api_version", MockStripeClient.MockApiVersion),
                ("payload_hash", StripeEventPayloadHasher.Hash(archivedPayload)),
                ("stripe_created_at", archivedOccurredAt)
            ]
        );
    }
}
