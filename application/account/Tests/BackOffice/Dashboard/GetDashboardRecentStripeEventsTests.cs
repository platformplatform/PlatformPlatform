using System.Net;
using System.Net.Http.Json;
using Account.Features.BackOffice.Dashboard.Queries;
using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using FluentAssertions;
using SharedKernel.Authentication.MockEasyAuth;
using SharedKernel.Domain;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.BackOffice.Dashboard;

public sealed class GetDashboardRecentStripeEventsTests : BackOfficeEndpointBaseTest
{
    [Fact]
    public async Task GetDashboardRecentStripeEvents_WhenCalled_ShouldReturnEventsFromBillingEventLog()
    {
        // Arrange — seed two billing events for one tenant: a subscription created event and a later upgrade.
        // The handler reads them straight from the log and returns them ordered by OccurredAt DESC.
        var now = DateTimeOffset.UtcNow;
        var tenantId = SeedTenant("Stripe Co");
        var subscriptionId = SubscriptionId.NewId();
        SeedBillingEvent(tenantId, subscriptionId, BillingEventType.SubscriptionCreated, now.AddHours(-3), "evt_created", toPlan: SubscriptionPlan.Standard);
        SeedBillingEvent(tenantId, subscriptionId, BillingEventType.SubscriptionUpgraded, now.AddHours(-1), "evt_upgraded", SubscriptionPlan.Standard, SubscriptionPlan.Premium, 30m, "DKK");

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/recent-stripe-events?Limit=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeDashboardRecentStripeEventsResponse>();
        payload.Should().NotBeNull();
        payload.Events.Should().HaveCount(2);
        payload.Events[0].Type.Should().Be(BillingEventType.SubscriptionUpgraded, "the most recent event must come first");
        payload.Events[0].FromPlan.Should().Be(SubscriptionPlan.Standard);
        payload.Events[0].ToPlan.Should().Be(SubscriptionPlan.Premium);
        payload.Events[0].AmountDelta.Should().Be(30m);
        payload.Events[1].Type.Should().Be(BillingEventType.SubscriptionCreated);
        payload.Events.Should().AllSatisfy(e => e.TenantName.Should().Be("Stripe Co"));
        payload.Events[0].OccurredAt.Should().BeAfter(payload.Events[^1].OccurredAt);
    }

    [Fact]
    public async Task GetDashboardRecentStripeEvents_WhenLimitIsApplied_ShouldReturnOnlyTheRequestedNumberOfRows()
    {
        // Arrange — three events; request only the two most recent.
        var now = DateTimeOffset.UtcNow;
        var tenantId = SeedTenant("Stripe Co");
        var subscriptionId = SubscriptionId.NewId();
        SeedBillingEvent(tenantId, subscriptionId, BillingEventType.SubscriptionCreated, now.AddHours(-5), "evt_a", toPlan: SubscriptionPlan.Standard);
        SeedBillingEvent(tenantId, subscriptionId, BillingEventType.SubscriptionRenewed, now.AddHours(-3), "evt_b", toPlan: SubscriptionPlan.Standard);
        SeedBillingEvent(tenantId, subscriptionId, BillingEventType.SubscriptionUpgraded, now.AddHours(-1), "evt_c", SubscriptionPlan.Standard, SubscriptionPlan.Premium);

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/recent-stripe-events?Limit=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeDashboardRecentStripeEventsResponse>();
        payload.Should().NotBeNull();
        payload.Events.Should().HaveCount(2);
        payload.Events[0].Type.Should().Be(BillingEventType.SubscriptionUpgraded);
        payload.Events[1].Type.Should().Be(BillingEventType.SubscriptionRenewed);
    }

    [Fact]
    public async Task GetDashboardRecentStripeEvents_WhenCalledWithInvalidLimit_ShouldReturnBadRequest()
    {
        // Arrange
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/recent-stripe-events?Limit=999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetDashboardRecentStripeEvents_WhenCalledWithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        using var client = CreateBackOfficeClient();

        // Act
        var response = await client.GetAsync("/api/back-office/dashboard/recent-stripe-events?Limit=6");

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
                ("plan", nameof(SubscriptionPlan.Standard)),
                ("logo", """{"Url":null,"Version":0}""")
            ]
        );
        return tenantId;
    }

    private void SeedBillingEvent(
        TenantId tenantId,
        SubscriptionId subscriptionId,
        BillingEventType eventType,
        DateTimeOffset occurredAt,
        string stripeReference,
        SubscriptionPlan? fromPlan = null,
        SubscriptionPlan? toPlan = null,
        decimal? amountDelta = null,
        string? currency = null
    )
    {
        var billingEvent = BillingEvent.Create(subscriptionId, tenantId, eventType, occurredAt, stripeReference, fromPlan, toPlan, amountDelta: amountDelta, currency: currency);
        Connection.Insert("billing_events", [
                ("tenant_id", tenantId.Value),
                ("id", billingEvent.Id.Value),
                ("subscription_id", subscriptionId.Value),
                ("created_at", DateTimeOffset.UtcNow),
                ("modified_at", null),
                ("event_type", eventType.ToString()),
                ("from_plan", fromPlan?.ToString()),
                ("to_plan", toPlan?.ToString()),
                ("previous_amount", null),
                ("new_amount", null),
                ("amount_delta", amountDelta),
                ("currency", currency),
                ("days_on_previous_plan", null),
                ("days_until_effective", null),
                ("days_since_cancelled", null),
                ("scheduled_for", null),
                ("effective_at", null),
                ("occurred_at", occurredAt),
                ("cancellation_reason", null),
                ("suspension_reason", null),
                ("stripe_reference", stripeReference)
            ]
        );
    }
}
