using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.BackOffice.Queries;
using Account.Features.Tenants.Domain;
using Account.Tests.BackOffice;
using FluentAssertions;
using SharedKernel.Authentication.BackOfficeIdentity;
using SharedKernel.Authentication.MockEasyAuth;
using SharedKernel.Domain;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Tenants.BackOffice;

public sealed class GetTenantsTests : BackOfficeEndpointBaseTest
{
    [Fact]
    public async Task GetTenants_WhenCalled_ShouldReturnAllTenantsWithSummaryFields()
    {
        // Arrange
        SeedTenant("Acme Corp", SubscriptionPlan.Standard, 49.99m, "USD", "US", false, null, 30);
        var tenantB = SeedTenant("Beta Industries", SubscriptionPlan.Premium, 199.00m, "EUR", "DE", true, null, 20);
        var tenantC = SeedTenant("Cyrus Co", SubscriptionPlan.Basis, null, null, null, false, SubscriptionPlan.Standard, 10);
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/tenants");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<TenantsResponse>();
        payload.Should().NotBeNull();
        // DatabaseSeeder.Tenant1 is also returned alongside the three tenants seeded by this test class.
        payload.TotalCount.Should().Be(4);
        payload.Tenants.Should().HaveCount(4);

        var beta = payload.Tenants.Single(t => t.Id == tenantB);
        beta.Name.Should().Be("Beta Industries");
        beta.Plan.Should().Be(SubscriptionPlan.Premium);
        beta.MonthlyRecurringRevenue.Should().Be(199.00m);
        beta.Currency.Should().Be("EUR");
        beta.Country.Should().Be("DE");
        beta.PlannedChange.Should().Be(PlannedSubscriptionChange.Cancellation);

        var cyrus = payload.Tenants.Single(t => t.Id == tenantC);
        cyrus.PlannedChange.Should().Be(PlannedSubscriptionChange.ScheduledPlanChange);
        cyrus.MonthlyRecurringRevenue.Should().BeNull();
    }

    [Fact]
    public async Task GetTenants_WhenSearchingByName_ShouldReturnMatchingTenants()
    {
        // Arrange
        var tenantA = SeedTenant("Acme Corp", SubscriptionPlan.Standard, 49.99m, "USD", "US", false, null, 30);
        SeedTenant("Beta Industries", SubscriptionPlan.Premium, 199.00m, "EUR", "DE", true, null, 20);
        SeedTenant("Cyrus Co", SubscriptionPlan.Basis, null, null, null, false, SubscriptionPlan.Standard, 10);
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/tenants?search=acme");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<TenantsResponse>();
        payload.Should().NotBeNull();
        payload.TotalCount.Should().Be(1);
        payload.Tenants.Single().Id.Should().Be(tenantA);
    }

    [Fact]
    public async Task GetTenants_WhenSearchingByExactId_ShouldReturnMatchingTenant()
    {
        // Arrange
        var tenantA = SeedTenant("Acme Corp", SubscriptionPlan.Standard, 49.99m, "USD", "US", false, null, 30);
        SeedTenant("Beta Industries", SubscriptionPlan.Premium, 199.00m, "EUR", "DE", true, null, 20);
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/tenants?search={tenantA.Value}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<TenantsResponse>();
        payload.Should().NotBeNull();
        payload.TotalCount.Should().Be(1);
        payload.Tenants.Single().Id.Should().Be(tenantA);
    }

    [Fact]
    public async Task GetTenants_WhenFilteringByPlan_ShouldReturnOnlyMatchingPlan()
    {
        // Arrange
        SeedTenant("Acme Corp", SubscriptionPlan.Standard, 49.99m, "USD", "US", false, null, 30);
        var tenantB = SeedTenant("Beta Industries", SubscriptionPlan.Premium, 199.00m, "EUR", "DE", true, null, 20);
        SeedTenant("Cyrus Co", SubscriptionPlan.Basis, null, null, null, false, SubscriptionPlan.Standard, 10);
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/tenants?plan=Premium");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<TenantsResponse>();
        payload.Should().NotBeNull();
        payload.TotalCount.Should().Be(1);
        payload.Tenants.Single().Id.Should().Be(tenantB);
    }

    [Fact]
    public async Task GetTenants_WhenSortingByMonthlyRecurringRevenueDescending_ShouldReturnHighestFirst()
    {
        // Arrange
        var tenantA = SeedTenant("Acme Corp", SubscriptionPlan.Standard, 49.99m, "USD", "US", false, null, 30);
        var tenantB = SeedTenant("Beta Industries", SubscriptionPlan.Premium, 199.00m, "EUR", "DE", true, null, 20);
        var tenantC = SeedTenant("Cyrus Co", SubscriptionPlan.Basis, null, null, null, false, SubscriptionPlan.Standard, 10);
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/tenants?orderBy=MonthlyRecurringRevenue&sortOrder=Descending");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<TenantsResponse>();
        payload.Should().NotBeNull();
        payload.Tenants.Select(t => t.Id).Should().ContainInOrder(tenantB, tenantA, tenantC);
    }

    [Fact]
    public async Task GetTenants_WhenSortingByCreatedAtAscending_ShouldReturnOldestFirst()
    {
        // Arrange
        var tenantA = SeedTenant("Acme Corp", SubscriptionPlan.Standard, 49.99m, "USD", "US", false, null, 30);
        var tenantB = SeedTenant("Beta Industries", SubscriptionPlan.Premium, 199.00m, "EUR", "DE", true, null, 20);
        var tenantC = SeedTenant("Cyrus Co", SubscriptionPlan.Basis, null, null, null, false, SubscriptionPlan.Standard, 10);
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/tenants?orderBy=CreatedAt&sortOrder=Ascending");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<TenantsResponse>();
        payload.Should().NotBeNull();
        payload.Tenants.Select(t => t.Id).Should().ContainInOrder(tenantA, tenantB, tenantC);
    }

    [Fact]
    public async Task GetTenants_WhenPagingBeyondAvailable_ShouldReturnBadRequest()
    {
        // Arrange
        SeedTenant("Acme Corp", SubscriptionPlan.Standard, 49.99m, "USD", "US", false, null, 30);
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/tenants?pageOffset=5&pageSize=25");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetTenants_WhenPagingWithSize_ShouldReturnPagedSlice()
    {
        // Arrange
        SeedTenant("Acme Corp", SubscriptionPlan.Standard, 49.99m, "USD", "US", false, null, 30);
        SeedTenant("Beta Industries", SubscriptionPlan.Premium, 199.00m, "EUR", "DE", true, null, 20);
        SeedTenant("Cyrus Co", SubscriptionPlan.Basis, null, null, null, false, SubscriptionPlan.Standard, 10);
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/tenants?pageSize=2&orderBy=Name&sortOrder=Ascending");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<TenantsResponse>();
        payload.Should().NotBeNull();
        // DatabaseSeeder.Tenant1 is also returned alongside the three tenants seeded by this test class.
        payload.TotalCount.Should().Be(4);
        payload.TotalPages.Should().Be(2);
        payload.Tenants.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTenants_WhenCalledWithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        using var client = CreateBackOfficeClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Act
        var response = await client.GetAsync("/api/back-office/tenants");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTenants_WhenCalledViaWrongHost_ShouldReturnNotFound()
    {
        // Arrange
        using var client = CreateClientForHost("app.test.localhost");
        client.DefaultRequestHeaders.Add(BackOfficeIdentityDefaults.PrincipalNameHeader, "Some User");

        // Act
        var response = await client.GetAsync("/api/back-office/tenants");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private TenantId SeedTenant(string name, SubscriptionPlan plan, decimal? mrr, string? currency, string? country, bool cancelAtPeriodEnd, SubscriptionPlan? scheduledPlan, int createdMinutesAgo)
    {
        var tenantId = TenantId.NewId();

        Connection.Insert("tenants", [
                ("id", tenantId.Value),
                ("created_at", DateTimeOffset.UtcNow.AddMinutes(-createdMinutesAgo)),
                ("modified_at", null),
                ("name", name),
                ("state", nameof(TenantState.Active)),
                ("plan", plan.ToString()),
                ("logo", """{"Url":null,"Version":0}""")
            ]
        );

        var billingInfoJson = country is null
            ? null
            : JsonSerializer.Serialize(new BillingInfo(name, new BillingAddress(null, null, null, null, null, country), null, null));

        Connection.Insert("subscriptions", [
                ("tenant_id", tenantId.Value),
                ("id", SubscriptionId.NewId().ToString()),
                ("created_at", DateTimeOffset.UtcNow.AddMinutes(-createdMinutesAgo)),
                ("modified_at", null),
                ("plan", plan.ToString()),
                ("scheduled_plan", scheduledPlan?.ToString()),
                ("stripe_customer_id", mrr is null ? null : "cus_test"),
                ("stripe_subscription_id", mrr is null ? null : "sub_test"),
                ("current_price_amount", (object?)mrr ?? DBNull.Value),
                ("current_price_currency", currency),
                ("current_period_end", mrr is null ? null : DateTimeOffset.UtcNow.AddDays(30)),
                ("cancel_at_period_end", cancelAtPeriodEnd),
                ("first_payment_failed_at", null),
                ("cancellation_reason", null),
                ("cancellation_feedback", null),
                ("payment_transactions", "[]"),
                ("payment_method", null),
                ("billing_info", billingInfoJson)
            ]
        );

        return tenantId;
    }
}
