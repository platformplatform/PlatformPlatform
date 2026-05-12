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

public sealed class GetTenantUsersTests : BackOfficeEndpointBaseTest
{
    [Fact]
    public async Task GetTenantUsers_WhenCalled_ShouldReturnUsersForThatTenantOnly()
    {
        // Arrange
        var tenant = DatabaseSeeder.Tenant1;
        SeedUser(tenant.Id, "alice@tenant-1.com", "Alice", "Anders", UserRole.Owner);
        SeedUser(tenant.Id, "bob@tenant-1.com", "Bob", "Bear", UserRole.Member);
        // Different tenant - should not be returned
        var otherTenantId = TenantId.NewId();
        Connection.Insert("tenants", [
                ("id", otherTenantId.Value),
                ("created_at", DateTimeOffset.UtcNow),
                ("modified_at", null),
                ("name", "Other"),
                ("state", "Active"),
                ("plan", "Basis"),
                ("logo", """{"Url":null,"Version":0}""")
            ]
        );
        SeedUser(otherTenantId, "outsider@other.com", "Outsider", null, UserRole.Member);

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/tenants/{tenant.Id}/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<TenantUsersResponse>();
        payload.Should().NotBeNull();
        // Tenant1 is seeded with Owner + Member from DatabaseSeeder, plus alice and bob.
        payload.TotalCount.Should().Be(4);
        payload.Users.Should().NotContain(u => u.Email.EndsWith("@other.com"));
    }

    [Fact]
    public async Task GetTenantUsers_WhenSearching_ShouldReturnMatchingUsers()
    {
        // Arrange
        var tenant = DatabaseSeeder.Tenant1;
        SeedUser(tenant.Id, "charlie@tenant-1.com", "Charlie", "Carter", UserRole.Member);

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/tenants/{tenant.Id}/users?search=charlie");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<TenantUsersResponse>();
        payload.Should().NotBeNull();
        payload.TotalCount.Should().Be(1);
        payload.Users.Single().Email.Should().Be("charlie@tenant-1.com");
    }

    [Fact]
    public async Task GetTenantUsers_WhenFilteringByOwnerRole_ShouldReturnOnlyOwners()
    {
        // Arrange
        var tenant = DatabaseSeeder.Tenant1;
        SeedUser(tenant.Id, "owner2@tenant-1.com", "Owen", "Two", UserRole.Owner);
        SeedUser(tenant.Id, "member1@tenant-1-extra.com", "Mike", "One", UserRole.Member);

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/tenants/{tenant.Id}/users?roles=Owner");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<TenantUsersResponse>();
        payload.Should().NotBeNull();
        // DatabaseSeeder.Tenant1Owner plus owner2 above.
        payload.TotalCount.Should().Be(2);
        payload.Users.Should().OnlyContain(u => u.Role == UserRole.Owner);
    }

    [Fact]
    public async Task GetTenantUsers_WhenFilteringByMultipleRoles_ShouldHonorRoleFilterAtQueryTimeAcrossPages()
    {
        // seed enough users across roles so role filtering must run at the database layer for
        // pagination to be correct. With 1 Owner on Tenant1 (the seeder) plus 30 Member users added below,
        // filtering ?roles=Owner must return only the 1 Owner regardless of page size.
        // Arrange
        var tenant = DatabaseSeeder.Tenant1;
        for (var index = 0; index < 30; index++)
        {
            SeedUser(tenant.Id, $"member{index}@tenant-1.com", $"Member{index}", "User", UserRole.Member);
        }

        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/tenants/{tenant.Id}/users?roles=Owner&pageSize=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<TenantUsersResponse>();
        payload.Should().NotBeNull();
        // DatabaseSeeder seeds one Owner and one Member on Tenant1; the loop adds 30 Members.
        payload.TotalCount.Should().Be(1);
        payload.Users.Should().OnlyContain(u => u.Role == UserRole.Owner);
    }

    [Fact]
    public async Task GetTenantUsers_WhenTenantNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var tenantId = TenantId.NewId();
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/tenants/{tenantId}/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private void SeedUser(TenantId tenantId, string email, string? firstName, string? lastName, UserRole role)
    {
        Connection.Insert("users", [
                ("tenant_id", tenantId.Value),
                ("id", UserId.NewId().ToString()),
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
                ("avatar", JsonSerializer.Serialize(new Avatar()))
            ]
        );
    }
}
