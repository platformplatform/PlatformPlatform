using System.Net;
using System.Net.Http.Json;
using Account.Features.BackOffice.BillingEvents.Queries;
using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using FluentAssertions;
using SharedKernel.Authentication.MockEasyAuth;
using SharedKernel.Domain;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.BackOffice;

public sealed class GetBackOfficeBillingEventsTests : BackOfficeEndpointBaseTest
{
    [Fact]
    public async Task GetBackOfficeBillingEvents_WhenCalled_ShouldReturnAllEventsOrderedByOccurredAtDescending()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var tenantA = SeedTenant("Acme");
        var tenantB = SeedTenant("Bravo");
        var subscriptionA = SubscriptionId.NewId();
        var subscriptionB = SubscriptionId.NewId();
        SeedBillingEvent(tenantA, subscriptionA, BillingEventType.SubscriptionCreated, now.AddHours(-3), "evt_a1", toPlan: SubscriptionPlan.Standard);
        SeedBillingEvent(tenantB, subscriptionB, BillingEventType.SubscriptionUpgraded, now.AddHours(-1), "evt_b1", SubscriptionPlan.Standard, SubscriptionPlan.Premium, 30m);
        SeedBillingEvent(tenantA, subscriptionA, BillingEventType.SubscriptionRenewed, now.AddHours(-2), "evt_a2", toPlan: SubscriptionPlan.Standard);

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/billing-events?PageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BillingEventsResponse>();
        payload.Should().NotBeNull();
        payload.TotalCount.Should().Be(3);
        payload.BillingEvents.Should().HaveCount(3);
        payload.BillingEvents[0].EventType.Should().Be(BillingEventType.SubscriptionUpgraded);
        payload.BillingEvents[0].TenantName.Should().Be("Bravo");
        payload.BillingEvents[0].FromPlan.Should().Be(SubscriptionPlan.Standard);
        payload.BillingEvents[0].ToPlan.Should().Be(SubscriptionPlan.Premium);
        payload.BillingEvents[0].AmountDelta.Should().Be(30m);
        payload.BillingEvents[^1].EventType.Should().Be(BillingEventType.SubscriptionCreated);
    }

    [Fact]
    public async Task GetBackOfficeBillingEvents_WhenFilteredByEventType_ShouldReturnOnlyMatchingEvents()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var tenant = SeedTenant("Acme");
        var subscriptionId = SubscriptionId.NewId();
        SeedBillingEvent(tenant, subscriptionId, BillingEventType.SubscriptionCreated, now.AddHours(-3), "evt_a1", toPlan: SubscriptionPlan.Standard);
        SeedBillingEvent(tenant, subscriptionId, BillingEventType.SubscriptionUpgraded, now.AddHours(-1), "evt_a2", SubscriptionPlan.Standard, SubscriptionPlan.Premium);
        SeedBillingEvent(tenant, subscriptionId, BillingEventType.SubscriptionRenewed, now.AddHours(-2), "evt_a3", toPlan: SubscriptionPlan.Standard);

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/billing-events?EventTypes=SubscriptionUpgraded&PageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BillingEventsResponse>();
        payload.Should().NotBeNull();
        payload.TotalCount.Should().Be(1);
        payload.BillingEvents.Should().ContainSingle().Which.EventType.Should().Be(BillingEventType.SubscriptionUpgraded);
    }

    [Fact]
    public async Task GetBackOfficeBillingEvents_WhenFilteredBySearch_ShouldReturnOnlyEventsForMatchingTenants()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var tenantA = SeedTenant("Acme");
        var tenantB = SeedTenant("Bravo");
        var subscriptionA = SubscriptionId.NewId();
        var subscriptionB = SubscriptionId.NewId();
        SeedBillingEvent(tenantA, subscriptionA, BillingEventType.SubscriptionCreated, now.AddHours(-3), "evt_a1", toPlan: SubscriptionPlan.Standard);
        SeedBillingEvent(tenantB, subscriptionB, BillingEventType.SubscriptionCreated, now.AddHours(-2), "evt_b1", toPlan: SubscriptionPlan.Standard);

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/billing-events?Search=Acme&PageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BillingEventsResponse>();
        payload.Should().NotBeNull();
        payload.TotalCount.Should().Be(1);
        payload.BillingEvents.Should().ContainSingle().Which.TenantName.Should().Be("Acme");
    }

    [Fact]
    public async Task GetBackOfficeBillingEvents_WhenPaginated_ShouldReturnRequestedPage()
    {
        // Arrange — five events across two tenants. Page size 2 means three pages of 2/2/1.
        var now = DateTimeOffset.UtcNow;
        var tenant = SeedTenant("Acme");
        var subscriptionId = SubscriptionId.NewId();
        for (var i = 0; i < 5; i++)
        {
            SeedBillingEvent(tenant, subscriptionId, BillingEventType.SubscriptionRenewed, now.AddHours(-i - 1), $"evt_{i}", toPlan: SubscriptionPlan.Standard);
        }

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var firstPage = await client.GetFromJsonAsync<BillingEventsResponse>("/api/back-office/billing-events?PageOffset=0&PageSize=2");
        var secondPage = await client.GetFromJsonAsync<BillingEventsResponse>("/api/back-office/billing-events?PageOffset=1&PageSize=2");

        // Assert
        firstPage.Should().NotBeNull();
        firstPage.TotalCount.Should().Be(5);
        firstPage.TotalPages.Should().Be(3);
        firstPage.BillingEvents.Should().HaveCount(2);
        secondPage.Should().NotBeNull();
        secondPage.BillingEvents.Should().HaveCount(2);
        secondPage.BillingEvents[0].Id.Should().NotBe(firstPage.BillingEvents[0].Id);
    }

    [Fact]
    public async Task GetBackOfficeBillingEvents_WhenFilteredByDateRange_ShouldReturnOnlyEventsWithinRange()
    {
        // Arrange — three events at now-3h, now-2h, now-1h. With OccurredFrom=now-2h we expect the latter two.
        var now = DateTimeOffset.UtcNow;
        var tenant = SeedTenant("Acme");
        var subscriptionId = SubscriptionId.NewId();
        SeedBillingEvent(tenant, subscriptionId, BillingEventType.SubscriptionCreated, now.AddHours(-3), "evt_old", toPlan: SubscriptionPlan.Standard);
        SeedBillingEvent(tenant, subscriptionId, BillingEventType.SubscriptionRenewed, now.AddHours(-2), "evt_mid", toPlan: SubscriptionPlan.Standard);
        SeedBillingEvent(tenant, subscriptionId, BillingEventType.SubscriptionUpgraded, now.AddHours(-1), "evt_new", SubscriptionPlan.Standard, SubscriptionPlan.Premium);

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        var occurredFrom = Uri.EscapeDataString(now.AddHours(-2).ToString("O"));

        // Act
        var response = await client.GetAsync($"/api/back-office/billing-events?OccurredFrom={occurredFrom}&PageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BillingEventsResponse>();
        payload.Should().NotBeNull();
        payload.TotalCount.Should().Be(2, "OccurredFrom is inclusive of the boundary, so events at now-2h and now-1h match");
        payload.BillingEvents.Should().AllSatisfy(e => e.OccurredAt.Should().BeOnOrAfter(now.AddHours(-2).AddSeconds(-1)));
    }

    [Fact]
    public async Task GetBackOfficeBillingEvents_WhenSortedAscendingByOccurredAt_ShouldReturnOldestFirst()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        var tenant = SeedTenant("Acme");
        var subscriptionId = SubscriptionId.NewId();
        SeedBillingEvent(tenant, subscriptionId, BillingEventType.SubscriptionCreated, now.AddHours(-3), "evt_old", toPlan: SubscriptionPlan.Standard);
        SeedBillingEvent(tenant, subscriptionId, BillingEventType.SubscriptionUpgraded, now.AddHours(-1), "evt_new", SubscriptionPlan.Standard, SubscriptionPlan.Premium);

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/billing-events?OrderBy=OccurredAt&SortOrder=Ascending&PageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BillingEventsResponse>();
        payload.Should().NotBeNull();
        payload.BillingEvents[0].EventType.Should().Be(BillingEventType.SubscriptionCreated, "ascending order should put the oldest event first");
        payload.BillingEvents[^1].EventType.Should().Be(BillingEventType.SubscriptionUpgraded);
    }

    [Fact]
    public async Task GetBackOfficeBillingEvents_WhenLogIsEmpty_ShouldReturnEmptyResponse()
    {
        // Arrange
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/billing-events?PageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BillingEventsResponse>();
        payload.Should().NotBeNull();
        payload.TotalCount.Should().Be(0);
        payload.BillingEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task GetBackOfficeBillingEvents_WhenSearchIsTooLong_ShouldReturnBadRequest()
    {
        // Arrange
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        var oversizedSearch = new string('a', 101);

        // Act
        var response = await client.GetAsync($"/api/back-office/billing-events?Search={oversizedSearch}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetBackOfficeBillingEvents_WhenPageSizeExceedsMax_ShouldReturnBadRequest()
    {
        // Arrange
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/billing-events?PageSize=101");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetBackOfficeBillingEvents_WhenCalledWithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        using var client = CreateBackOfficeClient();

        // Act
        var response = await client.GetAsync("/api/back-office/billing-events?PageSize=10");

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
        string? currency = "DKK"
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
