using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using Account.Features.Users.BackOffice.Queries;
using Account.Features.Users.Domain;
using Account.Tests.BackOffice;
using FluentAssertions;
using SharedKernel.Authentication.BackOfficeIdentity;
using SharedKernel.Authentication.MockEasyAuth;
using SharedKernel.Domain;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Users.BackOffice;

public sealed class GetBackOfficeUserDetailTests(BackOfficeWebApplicationFactory factory) : BackOfficeEndpointBaseTest(factory), IClassFixture<BackOfficeWebApplicationFactory>
{
    [Fact]
    public async Task GetBackOfficeUserDetail_WhenUserExists_ShouldReturnFullProfile()
    {
        // Arrange
        var user = DatabaseSeeder.Tenant1Owner;
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/users/{user.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeUserDetailResponse>();
        payload.Should().NotBeNull();
        payload.Id.Should().Be(user.Id);
        payload.Email.Should().Be(user.Email);
        payload.Role.Should().Be(UserRole.Owner);
        payload.TenantId.Should().Be(DatabaseSeeder.Tenant1.Id);
        payload.TenantName.Should().Be(DatabaseSeeder.Tenant1.Name);
    }

    [Fact]
    public async Task GetBackOfficeUserDetail_WhenUserBelongsToMultipleTenants_ShouldReturnAllTenantMemberships()
    {
        // Arrange
        var sharedEmail = "shared@example.com";
        var primaryUserId = SeedUser(DatabaseSeeder.Tenant1.Id, sharedEmail, "Shared", "User", UserRole.Member);
        var otherTenantId = SeedTenant("Other Tenant");
        SeedUser(otherTenantId, sharedEmail, "Shared", "User", UserRole.Owner);
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/users/{primaryUserId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<BackOfficeUserDetailResponse>();
        payload.Should().NotBeNull();
        payload.TenantMemberships.Should().HaveCount(2);
        payload.TenantMemberships.Should().Contain(m => m.TenantName == DatabaseSeeder.Tenant1.Name);
        payload.TenantMemberships.Should().Contain(m => m.TenantName == "Other Tenant" && m.Role == UserRole.Owner);
        payload.TenantMemberships.Should().OnlyContain(m => m.Plan == SubscriptionPlan.Basis);
        payload.TenantMemberships.Should().OnlyContain(m => m.PlannedChange == null);
        payload.TenantMemberships.Should().OnlyContain(m => m.HasEverSubscribed == false);
        payload.TenantMemberships.Should().OnlyContain(m => m.MonthlyRecurringRevenue == null);
        payload.TenantMemberships.Should().OnlyContain(m => m.ScheduledPriceAmount == null);
        payload.TenantMemberships.Should().OnlyContain(m => m.RenewalDate == null);
        payload.TenantMemberships.Should().OnlyContain(m => m.Currency == null);
        payload.TenantMemberships.Should().OnlyContain(m => m.Country == null);
        payload.TenantMemberships.Should().OnlyContain(m => m.TenantLogoUrl == null);
    }

    [Fact]
    public async Task GetBackOfficeUserDetail_WhenUserNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var unknownUserId = UserId.NewId();
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/users/{unknownUserId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetBackOfficeUserDetail_WhenCalledWithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange
        using var client = CreateBackOfficeClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Act
        var response = await client.GetAsync($"/api/back-office/users/{DatabaseSeeder.Tenant1Owner.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetBackOfficeUserDetail_WhenCalledViaWrongHost_ShouldReturnNotFound()
    {
        // Arrange
        using var client = CreateClientForHost("app.test.localhost");
        client.DefaultRequestHeaders.Add(BackOfficeIdentityDefaults.PrincipalNameHeader, "Some User");

        // Act
        var response = await client.GetAsync($"/api/back-office/users/{DatabaseSeeder.Tenant1Owner.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
                ("plan", nameof(SubscriptionPlan.Basis)),
                ("logo", """{"Url":null,"Version":0}"""),
                ("rollout_bucket", 50)
            ]
        );

        return tenantId;
    }

    private UserId SeedUser(TenantId tenantId, string email, string? firstName, string? lastName, UserRole role)
    {
        var userId = UserId.NewId();
        Connection.Insert("users", [
                ("tenant_id", tenantId.Value),
                ("id", userId.ToString()),
                ("created_at", DateTimeOffset.UtcNow.AddDays(-30)),
                ("modified_at", null),
                ("email", email),
                ("external_identities", "[]"),
                ("email_confirmed", true),
                ("first_name", firstName),
                ("last_name", lastName),
                ("title", null),
                ("role", role.ToString()),
                ("locale", "en-US"),
                ("avatar", JsonSerializer.Serialize(new Avatar())),
                ("rollout_bucket", 50)
            ]
        );
        return userId;
    }
}
