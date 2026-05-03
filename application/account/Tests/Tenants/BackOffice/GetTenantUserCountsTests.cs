using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Account.Features.Tenants.BackOffice.Queries;
using Account.Features.Users.Domain;
using Account.Tests.BackOffice;
using FluentAssertions;
using SharedKernel.Authentication.MockEasyAuth;
using SharedKernel.Domain;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Tenants.BackOffice;

public sealed class GetTenantUserCountsTests : BackOfficeEndpointBaseTest
{
    [Fact]
    public async Task GetTenantUserCounts_WhenCalled_ShouldReturnTotalAndActiveCounts()
    {
        // Arrange
        var tenant = DatabaseSeeder.Tenant1;
        SeedUser(tenant.Id, "active1@tenant-1.com", DateTimeOffset.UtcNow.AddDays(-1));
        SeedUser(tenant.Id, "active2@tenant-1.com", DateTimeOffset.UtcNow.AddDays(-15));
        SeedUser(tenant.Id, "inactive@tenant-1.com", DateTimeOffset.UtcNow.AddDays(-60));
        SeedUser(tenant.Id, "neverseen@tenant-1.com", null);

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/tenants/{tenant.Id}/user-counts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<TenantUserCountsResponse>();
        payload.Should().NotBeNull();
        // DatabaseSeeder seeds Tenant1 with two users (Owner, Member) plus our four; only the two seeded users have last_seen_at = null and the two recent ones above are active.
        payload.TotalUsers.Should().Be(6);
        payload.ActiveUsers.Should().Be(2);
    }

    [Fact]
    public async Task GetTenantUserCounts_WhenTenantNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var tenantId = TenantId.NewId();
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/tenants/{tenantId}/user-counts");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private void SeedUser(TenantId tenantId, string email, DateTimeOffset? lastSeenAt)
    {
        Connection.Insert("users", [
                ("tenant_id", tenantId.Value),
                ("id", UserId.NewId().ToString()),
                ("created_at", DateTimeOffset.UtcNow.AddDays(-30)),
                ("modified_at", null),
                ("last_seen_at", (object?)lastSeenAt ?? DBNull.Value),
                ("email", email),
                ("external_identities", "[]"),
                ("email_confirmed", true),
                ("first_name", null),
                ("last_name", null),
                ("title", null),
                ("role", nameof(UserRole.Member)),
                ("locale", "en-US"),
                ("avatar", JsonSerializer.Serialize(new Avatar()))
            ]
        );
    }
}
