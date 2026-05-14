using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using Account.Features.Users.BackOffice.Queries;
using Account.Features.Users.Domain;
using Account.Integrations.Stripe;
using Account.Tests.BackOffice;
using FluentAssertions;
using SharedKernel.Authentication.BackOfficeIdentity;
using SharedKernel.Authentication.MockEasyAuth;
using SharedKernel.Domain;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Users.BackOffice;

public sealed class GetBackOfficeUsersTests(BackOfficeWebApplicationFactory factory) : BackOfficeEndpointBaseTest(factory), IClassFixture<BackOfficeWebApplicationFactory>
{
    [Fact]
    public async Task GetBackOfficeUsers_WhenSearchByEmail_ShouldReturnMatchingUsersAcrossTenants()
    {
        // Arrange
        var tenantA = SeedTenant("Acme Corp");
        var tenantB = SeedTenant("Beta Industries");
        SeedUser(tenantA, "alice.unique@example.com", "Alice", "Anders", UserRole.Owner, true);
        SeedUser(tenantB, "bob.other@example.com", "Bob", "Bear", UserRole.Member, true);
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/users?search=alice.unique");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeUsersResponse>();
        payload.Should().NotBeNull();
        payload.TotalCount.Should().Be(1);
        payload.Users.Single().Email.Should().Be("alice.unique@example.com");
        payload.Users.Single().TenantName.Should().Be("Acme Corp");
    }

    [Fact]
    public async Task GetBackOfficeUsers_WhenSearchByFullName_ShouldReturnMatchingUsers()
    {
        // Arrange
        var tenant = SeedTenant("Acme Corp");
        SeedUser(tenant, "alice@acme.com", "Alice", "Wonderland", UserRole.Owner, true);
        SeedUser(tenant, "bob@acme.com", "Bob", "Builder", UserRole.Member, true);
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/users?search=alice%20wonderland");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeUsersResponse>();
        payload.Should().NotBeNull();
        payload.Users.Should().Contain(u => u.Email == "alice@acme.com");
        payload.Users.Should().NotContain(u => u.Email == "bob@acme.com");
    }

    [Fact]
    public async Task GetBackOfficeUsers_WhenSearchByTenantName_ShouldReturnUsersInThatTenant()
    {
        // Arrange
        var tenantA = SeedTenant("Cynical Solutions");
        var tenantB = SeedTenant("Other Co");
        SeedUser(tenantA, "alpha@cynical.com", "Alpha", "One", UserRole.Owner, true);
        SeedUser(tenantA, "beta@cynical.com", "Beta", "Two", UserRole.Member, true);
        SeedUser(tenantB, "gamma@other.com", "Gamma", "Three", UserRole.Member, true);
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/users?search=cynical");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeUsersResponse>();
        payload.Should().NotBeNull();
        payload.Users.Should().HaveCount(2);
        payload.Users.Should().OnlyContain(u => u.TenantName == "Cynical Solutions");
    }

    [Fact]
    public async Task GetBackOfficeUsers_WhenSearchIsMissing_ShouldReturnAllUsersMostRecentlySeenFirst()
    {
        // Arrange
        var tenant = SeedTenant("Listing Inc");
        SeedUser(tenant, "first@listing.com", "First", "User", UserRole.Owner, true,
            createdAt: DateTimeOffset.UtcNow.AddDays(-3), lastSeenAt: DateTimeOffset.UtcNow.AddDays(-3)
        );
        SeedUser(tenant, "middle@listing.com", "Middle", "User", UserRole.Member, true,
            createdAt: DateTimeOffset.UtcNow.AddDays(-2), lastSeenAt: DateTimeOffset.UtcNow.AddDays(-2)
        );
        SeedUser(tenant, "last@listing.com", "Last", "User", UserRole.Member, true,
            createdAt: DateTimeOffset.UtcNow.AddDays(-1), lastSeenAt: DateTimeOffset.UtcNow.AddDays(-1)
        );
        SeedUser(tenant, "never@listing.com", "Never", "Seen", UserRole.Member, true,
            createdAt: DateTimeOffset.UtcNow.AddDays(-1)
        );
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeUsersResponse>();
        payload.Should().NotBeNull();
        payload.Users.Length.Should().BeGreaterThanOrEqualTo(4);
        var seeded = payload.Users.Where(u => u.Email.EndsWith("@listing.com")).Select(u => u.Email).ToArray();
        seeded.Should().Equal("last@listing.com", "middle@listing.com", "first@listing.com", "never@listing.com");
    }

    [Fact]
    public async Task GetBackOfficeUsers_WhenFilteringByRoles_ShouldReturnOnlyMatchingRoles()
    {
        // Arrange
        var tenant = SeedTenant("Filterable Inc");
        SeedUser(tenant, "owner1@filterable.com", "Owen", "One", UserRole.Owner, true);
        SeedUser(tenant, "admin1@filterable.com", "Adam", "Admin", UserRole.Admin, true);
        SeedUser(tenant, "member1@filterable.com", "Mike", "Member", UserRole.Member, true);
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/users?search=filterable&roles=Owner&roles=Admin");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeUsersResponse>();
        payload.Should().NotBeNull();
        payload.Users.Should().HaveCount(2);
        payload.Users.Should().OnlyContain(u => u.Role == UserRole.Owner || u.Role == UserRole.Admin);
    }

    [Fact]
    public async Task GetBackOfficeUsers_WhenFilteringByActiveLast24Hours_ShouldReturnOnlyRecentlyActiveUsers()
    {
        // Arrange
        var tenant = SeedTenant("Activity Corp");
        var now = DateTimeOffset.UtcNow;
        SeedUser(tenant, "recent@activity.com", "Recent", "User", UserRole.Member, true, now.AddHours(-2));
        SeedUser(tenant, "old@activity.com", "Old", "User", UserRole.Member, true, now.AddDays(-3));
        SeedUser(tenant, "never@activity.com", "Never", "Seen", UserRole.Member, true);
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/users?search=activity&activity=ActiveLast24Hours");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeUsersResponse>();
        payload.Should().NotBeNull();
        payload.Users.Should().ContainSingle(u => u.Email == "recent@activity.com");
    }

    [Fact]
    public async Task GetBackOfficeUsers_WhenFilteringByInactiveOver30Days_ShouldIncludeNeverSeenUsers()
    {
        // Arrange
        var tenant = SeedTenant("Inactivity Corp");
        var now = DateTimeOffset.UtcNow;
        SeedUser(tenant, "recent@inactivity.com", "Recent", "User", UserRole.Member, true, now.AddDays(-2));
        SeedUser(tenant, "old@inactivity.com", "Old", "User", UserRole.Member, true, now.AddDays(-60));
        SeedUser(tenant, "never@inactivity.com", "Never", "Seen", UserRole.Member, true);
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/users?search=inactivity&activity=InactiveOver30Days");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeUsersResponse>();
        payload.Should().NotBeNull();
        payload.Users.Should().HaveCount(2);
        payload.Users.Should().Contain(u => u.Email == "old@inactivity.com");
        payload.Users.Should().Contain(u => u.Email == "never@inactivity.com");
    }

    [Fact]
    public async Task GetBackOfficeUsers_WhenPaging_ShouldReturnTotalCountAndPaging()
    {
        // Arrange
        var tenant = SeedTenant("Pageable LLC");
        for (var i = 0; i < 30; i++)
        {
            SeedUser(tenant, $"user{i:D2}@pageable.com", $"User{i:D2}", "Test", UserRole.Member, true);
        }

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/users?search=pageable&pageSize=10&pageOffset=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeUsersResponse>();
        payload.Should().NotBeNull();
        payload.TotalCount.Should().Be(30);
        payload.PageSize.Should().Be(10);
        payload.TotalPages.Should().Be(3);
        payload.CurrentPageOffset.Should().Be(2);
        payload.Users.Should().HaveCount(10);
    }

    [Fact]
    public async Task GetBackOfficeUsers_ShouldPopulateTenantStatusFields()
    {
        // Arrange
        var basisTenant = SeedTenant("Basis Plan Co");
        var standardTenant = SeedTenant("Standard Plan Co", SubscriptionPlan.Standard);
        SeedUser(basisTenant, "basis-user@example.com", "Basis", "User", UserRole.Owner, true);
        SeedUser(standardTenant, "standard-user@example.com", "Standard", "User", UserRole.Owner, true);
        // Standard plan tenant has a successful payment, so HasEverSubscribed should be true. Basis-plan tenant has no
        // subscription row at all, mirroring the "free / never paid" case.
        SeedSubscriptionWithSucceededPayment(standardTenant);
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var basisResponse = await client.GetAsync("/api/back-office/users?search=basis-user");
        var standardResponse = await client.GetAsync("/api/back-office/users?search=standard-user");

        // Assert
        var basisPayload = await basisResponse.Content.ReadFromJsonAsync<BackOfficeUsersResponse>();
        basisPayload.Should().NotBeNull();
        var basisRow = basisPayload.Users.Single();
        basisRow.TenantPlan.Should().Be(SubscriptionPlan.Basis);
        basisRow.TenantPlannedChange.Should().BeNull();
        basisRow.TenantHasEverSubscribed.Should().BeFalse();

        var standardPayload = await standardResponse.Content.ReadFromJsonAsync<BackOfficeUsersResponse>();
        standardPayload.Should().NotBeNull();
        var standardRow = standardPayload.Users.Single();
        standardRow.TenantPlan.Should().Be(SubscriptionPlan.Standard);
        standardRow.TenantPlannedChange.Should().BeNull();
        standardRow.TenantHasEverSubscribed.Should().BeTrue();
    }

    [Fact]
    public async Task GetBackOfficeUsers_WhenCalledWithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        using var client = CreateBackOfficeClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Act
        var response = await client.GetAsync("/api/back-office/users?search=anything");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetBackOfficeUsers_WhenCalledViaWrongHost_ShouldReturnNotFound()
    {
        // Arrange
        using var client = CreateClientForHost("app.test.localhost");
        client.DefaultRequestHeaders.Add(BackOfficeIdentityDefaults.PrincipalNameHeader, "Some User");

        // Act
        var response = await client.GetAsync("/api/back-office/users?search=anything");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private TenantId SeedTenant(string name, SubscriptionPlan plan = SubscriptionPlan.Basis)
    {
        var tenantId = TenantId.NewId();

        Connection.Insert("tenants", [
                ("id", tenantId.Value),
                ("created_at", DateTimeOffset.UtcNow.AddDays(-30)),
                ("modified_at", null),
                ("name", name),
                ("state", nameof(TenantState.Active)),
                ("plan", plan.ToString()),
                ("logo", """{"Url":null,"Version":0}"""),
                ("rollout_bucket", 50)
            ]
        );

        return tenantId;
    }

    private void SeedSubscriptionWithSucceededPayment(TenantId tenantId)
    {
        var paymentTransactionsJson = JsonSerializer.Serialize(new[]
            {
                new PaymentTransaction(PaymentTransactionId.NewId(), 49.99m, 49.99m, 0m, "USD", PaymentTransactionStatus.Succeeded, DateTimeOffset.UtcNow.AddDays(-30), null, null, null, SubscriptionPlan.Standard)
            }
        );

        Connection.Insert("subscriptions", [
                ("tenant_id", tenantId.Value),
                ("id", SubscriptionId.NewId().ToString()),
                ("created_at", DateTimeOffset.UtcNow.AddDays(-30)),
                ("modified_at", null),
                ("plan", nameof(SubscriptionPlan.Standard)),
                ("scheduled_plan", null),
                ("stripe_customer_id", "cus_test"),
                ("stripe_subscription_id", "sub_test"),
                ("current_price_amount", 49.99m),
                ("current_price_currency", MockStripeClient.MockStandardCurrency),
                ("current_period_end", DateTimeOffset.UtcNow.AddDays(30)),
                ("cancel_at_period_end", false),
                ("first_payment_failed_at", null),
                ("cancellation_reason", null),
                ("cancellation_feedback", null),
                ("payment_transactions", paymentTransactionsJson),
                ("payment_method", null),
                ("billing_info", null),
                ("has_drift_detected", false),
                ("drift_checked_at", null),
                ("drift_discrepancies", "[]")
            ]
        );
    }

    private void SeedUser(TenantId tenantId, string email, string? firstName, string? lastName, UserRole role, bool emailConfirmed, DateTimeOffset? lastSeenAt = null, DateTimeOffset? createdAt = null)
    {
        Connection.Insert("users", [
                ("tenant_id", tenantId.Value),
                ("id", UserId.NewId().ToString()),
                ("created_at", createdAt ?? DateTimeOffset.UtcNow.AddDays(-30)),
                ("modified_at", null),
                ("last_seen_at", lastSeenAt),
                ("email", email),
                ("external_identities", "[]"),
                ("email_confirmed", emailConfirmed),
                ("first_name", firstName),
                ("last_name", lastName),
                ("title", null),
                ("role", role.ToString()),
                ("locale", "en-US"),
                ("avatar", JsonSerializer.Serialize(new Avatar())),
                ("rollout_bucket", 50)
            ]
        );
    }
}
