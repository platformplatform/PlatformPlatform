using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Account.Features.BackOffice.BillingDrift.Queries;
using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using FluentAssertions;
using SharedKernel.Authentication.MockEasyAuth;
using SharedKernel.Domain;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.BackOffice.BillingDrift;

public sealed class GetUnsyncedSubscriptionsSummaryTests : BackOfficeEndpointBaseTest
{
    [Fact]
    public async Task GetUnsyncedSubscriptionsSummary_WhenCalled_ShouldReturnPaidSubscriptionsWithoutBillingEvents()
    {
        // Arrange — two paid subscriptions, only one has a billing event.
        var syncedTenant = SeedTenant("Synced Co");
        var syncedSubscriptionId = SubscriptionId.NewId();
        SeedPaidSubscription(syncedTenant, syncedSubscriptionId, 149m);
        SeedSubscriptionCreatedEvent(syncedTenant, syncedSubscriptionId, 149m);

        var unsyncedTenant = SeedTenant("Unsynced Co");
        SeedPaidSubscription(unsyncedTenant, SubscriptionId.NewId(), 299m);

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/billing-drift/unsynced-summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<UnsyncedSubscriptionsSummaryResponse>();
        payload.Should().NotBeNull();
        payload.UnsyncedSubscriptionsCount.Should().Be(1);
    }

    [Fact]
    public async Task GetUnsyncedSubscriptionsSummary_WhenCalledWithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        using var client = CreateBackOfficeClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Act
        var response = await client.GetAsync("/api/back-office/billing-drift/unsynced-summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private TenantId SeedTenant(string name)
    {
        var tenantId = TenantId.NewId();
        Connection.Insert("tenants", [
                ("id", tenantId.Value),
                ("created_at", DateTimeOffset.UtcNow.AddDays(-30)),
                ("modified_at", null),
                ("name", name),
                ("state", nameof(TenantState.Active)),
                ("plan", nameof(SubscriptionPlan.Premium)),
                ("logo", """{"Url":null,"Version":0}""")
            ]
        );
        return tenantId;
    }

    private void SeedPaidSubscription(TenantId tenantId, SubscriptionId subscriptionId, decimal currentPriceAmount)
    {
        Connection.Insert("subscriptions", [
                ("tenant_id", tenantId.Value),
                ("id", subscriptionId.ToString()),
                ("created_at", DateTimeOffset.UtcNow.AddDays(-30)),
                ("modified_at", null),
                ("plan", nameof(SubscriptionPlan.Premium)),
                ("scheduled_plan", null),
                ("stripe_customer_id", "cus_test"),
                ("stripe_subscription_id", "sub_test"),
                ("current_price_amount", currentPriceAmount),
                ("current_price_currency", "DKK"),
                ("current_period_end", DateTimeOffset.UtcNow.AddDays(30)),
                ("cancel_at_period_end", false),
                ("first_payment_failed_at", null),
                ("cancellation_reason", null),
                ("cancellation_feedback", null),
                ("payment_transactions", "[]"),
                ("payment_method", null),
                ("billing_info", null),
                ("scheduled_price_amount", null),
                ("has_drift_detected", false),
                ("drift_checked_at", null),
                ("drift_discrepancies", "[]")
            ]
        );
    }

    private void SeedSubscriptionCreatedEvent(TenantId tenantId, SubscriptionId subscriptionId, decimal newAmount)
    {
        var occurredAt = DateTimeOffset.UtcNow.AddDays(-30);
        Connection.Insert("billing_events", [
                ("tenant_id", tenantId.Value),
                ("id", BillingEventId.FromComponents(subscriptionId, BillingEventType.SubscriptionCreated, "evt_test", occurredAt).ToString()),
                ("subscription_id", subscriptionId.ToString()),
                ("created_at", occurredAt),
                ("modified_at", null),
                ("event_type", nameof(BillingEventType.SubscriptionCreated)),
                ("from_plan", null),
                ("to_plan", nameof(SubscriptionPlan.Premium)),
                ("previous_amount", 0m),
                ("new_amount", newAmount),
                ("amount_delta", newAmount),
                ("currency", "DKK"),
                ("days_on_previous_plan", null),
                ("days_until_effective", null),
                ("days_since_cancelled", null),
                ("scheduled_for", null),
                ("effective_at", null),
                ("occurred_at", occurredAt),
                ("cancellation_reason", null),
                ("suspension_reason", null),
                ("stripe_reference", "evt_test")
            ]
        );
    }
}
