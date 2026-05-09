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

public sealed class GetDashboardMrrConsistencySummaryTests : BackOfficeEndpointBaseTest
{
    [Fact]
    public async Task GetDashboardMrrConsistencySummary_WhenSubscriptionsAndEventsAgree_ShouldReturnEqualValues()
    {
        // Arrange — one paid subscription with a matching SubscriptionCreated billing event.
        var tenantId = SeedTenant("Healthy Co");
        var subscriptionId = SubscriptionId.NewId();
        SeedPaidSubscription(tenantId, subscriptionId, 149m, false, null);
        SeedSubscriptionCreatedEvent(tenantId, subscriptionId, 149m);

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/billing-drift/mrr-consistency-summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<DashboardMrrConsistencySummaryResponse>();
        payload.Should().NotBeNull();
        payload.KpiMonthlyRecurringRevenue.Should().Be(149m);
        payload.TrendLatestMonthlyRecurringRevenue.Should().Be(149m);
    }

    [Fact]
    public async Task GetDashboardMrrConsistencySummary_WhenSubscriptionCancelledButNoCancellationEvent_ShouldReturnDifferingValues()
    {
        // Arrange — paid subscription cancelled at period end (KPI forward MRR contribution = 0)
        // but the only billing event is SubscriptionCreated with NewAmount = 149 (trend latest = 149).
        // The endpoint exists to flag exactly this divergence.
        var tenantId = SeedTenant("Drifted Co");
        var subscriptionId = SubscriptionId.NewId();
        SeedPaidSubscription(tenantId, subscriptionId, 149m, true, null);
        SeedSubscriptionCreatedEvent(tenantId, subscriptionId, 149m);

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/billing-drift/mrr-consistency-summary");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<DashboardMrrConsistencySummaryResponse>();
        payload.Should().NotBeNull();
        payload.KpiMonthlyRecurringRevenue.Should().Be(0m);
        payload.TrendLatestMonthlyRecurringRevenue.Should().Be(149m);
    }

    [Fact]
    public async Task GetDashboardMrrConsistencySummary_WhenCalledWithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        using var client = CreateBackOfficeClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Act
        var response = await client.GetAsync("/api/back-office/billing-drift/mrr-consistency-summary");

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

    private void SeedPaidSubscription(TenantId tenantId, SubscriptionId subscriptionId, decimal currentPriceAmount, bool cancelAtPeriodEnd, decimal? scheduledPriceAmount)
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
                ("cancel_at_period_end", cancelAtPeriodEnd),
                ("first_payment_failed_at", null),
                ("cancellation_reason", null),
                ("cancellation_feedback", null),
                ("payment_transactions", "[]"),
                ("payment_method", null),
                ("billing_info", null),
                ("scheduled_price_amount", (object?)scheduledPriceAmount ?? DBNull.Value),
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
                ("id", BillingEventId.NewId().Value),
                ("subscription_id", subscriptionId.ToString()),
                ("created_at", occurredAt),
                ("modified_at", null),
                ("stripe_event_id", "evt_test"),
                ("event_type", nameof(BillingEventType.SubscriptionCreated)),
                ("from_plan", null),
                ("to_plan", nameof(SubscriptionPlan.Premium)),
                ("previous_amount", 0m),
                ("new_amount", newAmount),
                ("amount_delta", newAmount),
                ("committed_mrr", newAmount),
                ("currency", "DKK"),
                ("occurred_at", occurredAt),
                ("cancellation_reason", null),
                ("suspension_reason", null)
            ]
        );
    }
}
