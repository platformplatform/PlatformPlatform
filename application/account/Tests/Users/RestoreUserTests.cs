using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Account.Database;
using Account.Features.Users.Domain;
using Account.Features.Users.Shared;
using FluentAssertions;
using SharedKernel.Domain;
using SharedKernel.Tests;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Users;

public sealed class RestoreUserTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task RestoreUser_WhenOwnerRestoresDeletedUser_ShouldSucceed()
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
        var response = await AuthenticatedOwnerHttpClient.PostAsync($"/api/account/users/{deletedUserId}/restore", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userResponse = await response.Content.ReadFromJsonAsync<UserResponse>();
        userResponse.Should().NotBeNull();
        userResponse.Id.Should().Be(deletedUserId);
        var deletedAt = Connection.ExecuteScalar<string>("SELECT DeletedAt FROM Users WHERE Id = @id", [new { id = deletedUserId.ToString() }]);
        deletedAt.Should().BeNullOrEmpty();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("UserRestored");
    }

    [Fact]
    public async Task RestoreUser_WhenMember_ShouldReturnForbidden()
    {
        // Arrange
        var deletedUserId = UserId.NewId();

        // Act
        var response = await AuthenticatedMemberHttpClient.PostAsync($"/api/account/users/{deletedUserId}/restore", null);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only owners and admins can restore deleted users.");
    }

    [Fact]
    public async Task RestoreUser_WhenUserNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentUserId = UserId.NewId();

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsync($"/api/account/users/{nonExistentUserId}/restore", null);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"Deleted user with id '{nonExistentUserId}' not found.");
    }

    [Fact]
    public async Task RestoreUser_WhenUserNotDeleted_ShouldReturnNotFound()
    {
        // Arrange
        var activeUserId = UserId.NewId();
        Connection.Insert("Users", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", activeUserId.ToString()),
                ("CreatedAt", TimeProvider.GetUtcNow().AddDays(-10)),
                ("ModifiedAt", null),
                ("DeletedAt", null),
                ("Email", Faker.Internet.UniqueEmail()),
                ("FirstName", Faker.Person.FirstName),
                ("LastName", Faker.Person.LastName),
                ("Title", "Active Employee"),
                ("Role", nameof(UserRole.Member)),
                ("EmailConfirmed", true),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Locale", "en-US"),
                ("ExternalIdentities", "[]")
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsync($"/api/account/users/{activeUserId}/restore", null);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"Deleted user with id '{activeUserId}' not found.");
    }
}
