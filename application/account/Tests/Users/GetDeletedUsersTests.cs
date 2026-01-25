using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using PlatformPlatform.Account.Database;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.Account.Features.Users.Queries;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using Xunit;

namespace PlatformPlatform.Account.Tests.Users;

public sealed class GetDeletedUsersTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task GetDeletedUsers_WhenOwner_ShouldReturnDeletedUsers()
    {
        // Arrange
        var deletedUserId = UserId.NewId();
        Connection.Insert("Users", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", deletedUserId.ToString()),
                ("CreatedAt", TimeProvider.GetUtcNow().AddDays(-10)),
                ("ModifiedAt", TimeProvider.GetUtcNow().AddDays(-1)),
                ("DeletedAt", TimeProvider.GetUtcNow().AddDays(-1)),
                ("Email", Faker.Internet.UniqueEmail()),
                ("FirstName", Faker.Person.FirstName),
                ("LastName", Faker.Person.LastName),
                ("Title", "Former Employee"),
                ("Role", nameof(UserRole.Member)),
                ("EmailConfirmed", true),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Locale", "en-US"),
                ("ExternalIdentities", "[]")
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
