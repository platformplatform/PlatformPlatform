using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Account.Database;
using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using Account.Features.Tenants.Queries;
using Account.Features.Users.Domain;
using FluentAssertions;
using SharedKernel.Domain;
using SharedKernel.Tests;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Tenants;

public sealed class GetTenantsForUserTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task GetTenants_UserWithMultipleTenants_ReturnsAllTenants()
    {
        // Arrange
        var tenant2Id = TenantId.NewId();
        var tenant2Name = Faker.Company.CompanyName();
        var user2Id = UserId.NewId();

        Connection.Insert("tenants", [
                ("id", tenant2Id.Value),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("name", tenant2Name),
                ("state", nameof(TenantState.Active)),
                ("logo", """{"Url":null,"Version":0}"""),
                ("plan", nameof(SubscriptionPlan.Basis)),
                ("rollout_bucket", 42)
            ]
        );

        Connection.Insert("users", [
                ("tenant_id", tenant2Id.Value),
                ("id", user2Id.ToString()),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("email", DatabaseSeeder.Tenant1Member.Email),
                ("email_confirmed", true),
                ("first_name", Faker.Name.FirstName()),
                ("last_name", Faker.Name.LastName()),
                ("title", null),
                ("avatar", JsonSerializer.Serialize(new Avatar())),
                ("role", nameof(UserRole.Owner)),
                ("locale", "en-US"),
                ("external_identities", "[]"),
                ("rollout_bucket", 42)
            ]
        );

        // Act
        var response = await AuthenticatedMemberHttpClient.GetAsync("/api/account/tenants");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.Content.ReadFromJsonAsync<GetTenantsForUserResponse>();
        result.Should().NotBeNull();
        result.Tenants.Should().HaveCount(2);
        result.Tenants.Should().Contain(t => t.TenantId == DatabaseSeeder.Tenant1.Id && t.UserId == DatabaseSeeder.Tenant1Member.Id);
        result.Tenants.Should().Contain(t => t.TenantId == tenant2Id && t.TenantName == tenant2Name && t.UserId == user2Id);
    }

    [Fact]
    public async Task GetTenants_UserWithSingleTenant_ReturnsSingleTenant()
    {
        // Act
        var response = await AuthenticatedMemberHttpClient.GetAsync("/api/account/tenants");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.Content.ReadFromJsonAsync<GetTenantsForUserResponse>();
        result.Should().NotBeNull();
        result.Tenants.Should().HaveCount(1);
        result.Tenants[0].TenantId.Should().Be(DatabaseSeeder.Tenant1Member.TenantId);
        result.Tenants[0].TenantName.Should().Be(DatabaseSeeder.Tenant1.Name);
        result.Tenants[0].UserId.Should().Be(DatabaseSeeder.Tenant1Member.Id);
    }

    [Fact]
    public async Task GetTenants_Unauthenticated_ReturnsUnauthorized()
    {
        // Act
        var response = await AnonymousHttpClient.GetAsync("/api/account/tenants");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTenants_CurrentTenantIncluded_VerifyCurrentTenantInResponse()
    {
        // Arrange
        var email = DatabaseSeeder.Tenant1Member.Email;
        var currentTenantId = DatabaseSeeder.Tenant1.Id;
        var otherTenantId = TenantId.NewId();
        var otherUserId = UserId.NewId();

        Connection.Insert("tenants", [
                ("id", otherTenantId.Value),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("name", "Other Tenant"),
                ("state", nameof(TenantState.Active)),
                ("logo", """{"Url":null,"Version":0}"""),
                ("plan", nameof(SubscriptionPlan.Basis)),
                ("rollout_bucket", 42)
            ]
        );

        Connection.Insert("users", [
                ("tenant_id", otherTenantId.Value),
                ("id", otherUserId.ToString()),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("email", email),
                ("email_confirmed", true),
                ("first_name", Faker.Name.FirstName()),
                ("last_name", Faker.Name.LastName()),
                ("title", null),
                ("avatar", JsonSerializer.Serialize(new Avatar())),
                ("role", nameof(UserRole.Member)),
                ("locale", "en-US"),
                ("external_identities", "[]"),
                ("rollout_bucket", 42)
            ]
        );

        // Act
        var response = await AuthenticatedMemberHttpClient.GetAsync("/api/account/tenants");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.Content.ReadFromJsonAsync<GetTenantsForUserResponse>();
        result.Should().NotBeNull();
        result.Tenants.Should().Contain(t => t.TenantId == currentTenantId);
    }

    [Fact]
    public async Task GetTenants_UsersOnlySeeTheirOwnTenants_DoesNotReturnOtherUsersTenants()
    {
        // Arrange
        var otherUserEmail = Faker.Internet.UniqueEmail().ToLowerInvariant();
        var otherUserTenantId = TenantId.NewId();
        var otherUserId = UserId.NewId();

        Connection.Insert("tenants", [
                ("id", otherUserTenantId.Value),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("name", "Other User Tenant"),
                ("state", nameof(TenantState.Active)),
                ("logo", """{"Url":null,"Version":0}"""),
                ("plan", nameof(SubscriptionPlan.Basis)),
                ("rollout_bucket", 42)
            ]
        );

        Connection.Insert("users", [
                ("tenant_id", otherUserTenantId.Value),
                ("id", otherUserId.ToString()),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("email", otherUserEmail),
                ("email_confirmed", true),
                ("first_name", Faker.Name.FirstName()),
                ("last_name", Faker.Name.LastName()),
                ("title", null),
                ("avatar", JsonSerializer.Serialize(new Avatar())),
                ("role", nameof(UserRole.Member)),
                ("locale", "en-US"),
                ("external_identities", "[]"),
                ("rollout_bucket", 42)
            ]
        );

        // Act
        var response = await AuthenticatedMemberHttpClient.GetAsync("/api/account/tenants");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.Content.ReadFromJsonAsync<GetTenantsForUserResponse>();
        result.Should().NotBeNull();
        result.Tenants.Should().HaveCount(1);
        result.Tenants.Should().NotContain(t => t.TenantId == otherUserTenantId);
    }

    [Fact]
    public async Task GetTenants_UserWithUnconfirmedEmail_ShowsAsNewTenant()
    {
        // Arrange
        var tenant2Id = TenantId.NewId();
        var tenant2Name = Faker.Company.CompanyName();
        var user2Id = UserId.NewId();

        Connection.Insert("tenants", [
                ("id", tenant2Id.Value),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("name", tenant2Name),
                ("state", nameof(TenantState.Active)),
                ("logo", """{"Url":null,"Version":0}"""),
                ("plan", nameof(SubscriptionPlan.Basis)),
                ("rollout_bucket", 42)
            ]
        );

        Connection.Insert("users", [
                ("tenant_id", tenant2Id.Value),
                ("id", user2Id.ToString()),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("email", DatabaseSeeder.Tenant1Member.Email),
                ("email_confirmed", false), // User has not confirmed email in this tenant
                ("first_name", Faker.Name.FirstName()),
                ("last_name", Faker.Name.LastName()),
                ("title", null),
                ("avatar", JsonSerializer.Serialize(new Avatar())),
                ("role", nameof(UserRole.Member)),
                ("locale", "en-US"),
                ("external_identities", "[]"),
                ("rollout_bucket", 42)
            ]
        );

        // Act
        var response = await AuthenticatedMemberHttpClient.GetAsync("/api/account/tenants");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.Content.ReadFromJsonAsync<GetTenantsForUserResponse>();
        result.Should().NotBeNull();
        result.Tenants.Should().HaveCount(2);

        // Current tenant should not be marked as new (user is already logged in)
        var currentTenant = result.Tenants.Single(t => t.TenantId == DatabaseSeeder.Tenant1.Id);
        currentTenant.IsNew.Should().BeFalse();

        // New tenant with unconfirmed email should be marked as new
        var newTenant = result.Tenants.Single(t => t.TenantId == tenant2Id);
        newTenant.IsNew.Should().BeTrue();
    }
}
