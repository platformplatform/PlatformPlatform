using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.Authentication.Queries;
using PlatformPlatform.AccountManagement.Features.Tenants.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Authentication;

public sealed class GetTenantsForUserTests : EndpointBaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task GetTenants_UserWithMultipleTenants_ReturnsAllTenants()
    {
        // Arrange
        var tenant2Id = TenantId.NewId();
        var tenant2Name = Faker.Company.CompanyName();
        var user2Id = UserId.NewId();

        Connection.Insert("Tenants", [
                ("Id", tenant2Id.Value),
                ("CreatedAt", TimeProvider.System.GetUtcNow()),
                ("ModifiedAt", null),
                ("Name", tenant2Name),
                ("State", TenantState.Active.ToString()),
                ("Logo", """{"Url":null,"Version":0}""")
            ]
        );

        Connection.Insert("Users", [
                ("TenantId", tenant2Id.Value),
                ("Id", user2Id.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow()),
                ("ModifiedAt", null),
                ("Email", DatabaseSeeder.Tenant1Member.Email),
                ("EmailConfirmed", true),
                ("FirstName", Faker.Name.FirstName()),
                ("LastName", Faker.Name.LastName()),
                ("Title", null),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Role", UserRole.Owner.ToString()),
                ("Locale", "en-US")
            ]
        );

        // Act
        var response = await AuthenticatedMemberHttpClient.GetAsync("/api/account-management/authentication/tenants");

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
        var response = await AuthenticatedMemberHttpClient.GetAsync("/api/account-management/authentication/tenants");

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
        var response = await AnonymousHttpClient.GetAsync("/api/account-management/authentication/tenants");

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

        Connection.Insert("Tenants", [
                ("Id", otherTenantId.Value),
                ("CreatedAt", TimeProvider.System.GetUtcNow()),
                ("ModifiedAt", null),
                ("Name", "Other Tenant"),
                ("State", TenantState.Active.ToString()),
                ("Logo", """{"Url":null,"Version":0}""")
            ]
        );

        Connection.Insert("Users", [
                ("TenantId", otherTenantId.Value),
                ("Id", otherUserId.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow()),
                ("ModifiedAt", null),
                ("Email", email),
                ("EmailConfirmed", true),
                ("FirstName", Faker.Name.FirstName()),
                ("LastName", Faker.Name.LastName()),
                ("Title", null),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Role", UserRole.Member.ToString()),
                ("Locale", "en-US")
            ]
        );

        // Act
        var response = await AuthenticatedMemberHttpClient.GetAsync("/api/account-management/authentication/tenants");

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
        var otherUserEmail = Faker.Internet.Email().ToLowerInvariant();
        var otherUserTenantId = TenantId.NewId();
        var otherUserId = UserId.NewId();

        Connection.Insert("Tenants", [
                ("Id", otherUserTenantId.Value),
                ("CreatedAt", TimeProvider.System.GetUtcNow()),
                ("ModifiedAt", null),
                ("Name", "Other User Tenant"),
                ("State", TenantState.Active.ToString()),
                ("Logo", """{"Url":null,"Version":0}""")
            ]
        );

        Connection.Insert("Users", [
                ("TenantId", otherUserTenantId.Value),
                ("Id", otherUserId.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow()),
                ("ModifiedAt", null),
                ("Email", otherUserEmail),
                ("EmailConfirmed", true),
                ("FirstName", Faker.Name.FirstName()),
                ("LastName", Faker.Name.LastName()),
                ("Title", null),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Role", UserRole.Member.ToString()),
                ("Locale", "en-US")
            ]
        );

        // Act
        var response = await AuthenticatedMemberHttpClient.GetAsync("/api/account-management/authentication/tenants");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var result = await response.Content.ReadFromJsonAsync<GetTenantsForUserResponse>();
        result.Should().NotBeNull();
        result.Tenants.Should().HaveCount(1);
        result.Tenants.Should().NotContain(t => t.TenantId == otherUserTenantId);
    }
}
