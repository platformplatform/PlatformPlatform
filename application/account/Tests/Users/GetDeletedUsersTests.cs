using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Account.Database;
using Account.Features.Users.Domain;
using Account.Features.Users.Queries;
using FluentAssertions;
using SharedKernel.Domain;
using SharedKernel.Tests;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Users;

public sealed class GetDeletedUsersTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task GetDeletedUsers_WhenOwner_ShouldReturnDeletedUsers()
    {
        // Arrange
        var deletedUserId = UserId.NewId();
        Connection.Insert("users", [
                ("tenant_id", DatabaseSeeder.Tenant1.Id.ToString()),
                ("id", deletedUserId.ToString()),
                ("created_at", TimeProvider.GetUtcNow().AddDays(-10)),
                ("modified_at", TimeProvider.GetUtcNow().AddDays(-1)),
                ("deleted_at", TimeProvider.GetUtcNow().AddDays(-1)),
                ("email", Faker.Internet.UniqueEmail()),
                ("first_name", Faker.Person.FirstName),
                ("last_name", Faker.Person.LastName),
                ("title", "Former Employee"),
                ("role", nameof(UserRole.Member)),
                ("email_confirmed", true),
                ("avatar", JsonSerializer.Serialize(new Avatar())),
                ("locale", "en-US"),
                ("external_identities", "[]")
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account/users/deleted");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<DeletedUsersResponse>();
        result!.TotalCount.Should().Be(1);
        result.Users.Should().ContainSingle(u => u.Id == deletedUserId);
    }

    [Fact]
    public async Task GetDeletedUsers_WhenMember_ShouldReturnForbidden()
    {
        // Act
        var response = await AuthenticatedMemberHttpClient.GetAsync("/api/account/users/deleted");

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only owners and admins can view deleted users.");
    }

    [Fact]
    public async Task GetDeletedUsers_WhenNoDeletedUsers_ShouldReturnEmptyList()
    {
        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account/users/deleted");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<DeletedUsersResponse>();
        result!.TotalCount.Should().Be(0);
        result.Users.Should().BeEmpty();
    }
}
