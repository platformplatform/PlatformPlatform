using System.Net;
using System.Text.Json;
using FluentAssertions;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.EmailAuthentication.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Users;

public sealed class DeleteUserTests : EndpointBaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task DeleteUser_WhenUserDoesNotExists_ShouldReturnNotFound()
    {
        // Arrange
        var unknownUserId = UserId.NewId();

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/api/account-management/users/{unknownUserId}");

        //Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"User with id '{unknownUserId}' not found.");
    }

    [Fact]
    public async Task DeleteUser_WhenUserExists_ShouldSoftDeleteUser()
    {
        // Arrange
        var userId = UserId.NewId();
        Connection.Insert("Users", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", userId.ToString()),
                ("CreatedAt", TimeProvider.GetUtcNow().AddMinutes(-10)),
                ("ModifiedAt", null),
                ("DeletedAt", null),
                ("Email", Faker.Internet.UniqueEmail()),
                ("FirstName", Faker.Person.FirstName),
                ("LastName", Faker.Person.LastName),
                ("Title", "Philanthropist & Innovator"),
                ("Role", nameof(UserRole.Member)),
                ("EmailConfirmed", true),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Locale", "en-US")
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/api/account-management/users/{userId}");

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();
        Connection.RowExists("Users", userId.ToString()).Should().BeTrue();
        var deletedAt = Connection.ExecuteScalar<string>("SELECT DeletedAt FROM Users WHERE Id = @id", [new { id = userId.ToString() }]);
        deletedAt.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task DeleteUser_WhenDeletingOwnUSer_ShouldGetForbidden()
    {
        // Arrange
        var existingUserId = DatabaseSeeder.Tenant1Owner.Id;

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/api/account-management/users/{existingUserId}");

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "You cannot delete yourself.");
    }

    [Fact]
    public async Task DeleteUser_WhenUserHasEmailLoginHistory_ShouldSoftDeleteUserAndKeepEmailLogins()
    {
        // Arrange
        var userId = UserId.NewId();
        Connection.Insert("Users", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", userId.ToString()),
                ("CreatedAt", TimeProvider.GetUtcNow().AddMinutes(-10)),
                ("ModifiedAt", null),
                ("DeletedAt", null),
                ("Email", Faker.Internet.UniqueEmail()),
                ("FirstName", Faker.Person.FirstName),
                ("LastName", Faker.Person.LastName),
                ("Title", "Philanthropist & Innovator"),
                ("Role", nameof(UserRole.Member)),
                ("EmailConfirmed", true),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Locale", "en-US")
            ]
        );

        var email = Connection.ExecuteScalar<string>("SELECT Email FROM Users WHERE Id = @id", [new { id = userId.ToString() }]);
        var emailLoginId = EmailLoginId.NewId();
        Connection.Insert("EmailLogins", [
                ("Id", emailLoginId.ToString()),
                ("CreatedAt", TimeProvider.GetUtcNow().AddMinutes(-5)),
                ("ModifiedAt", null),
                ("Email", email),
                ("Type", nameof(EmailLoginType.Login)),
                ("OneTimePasswordHash", "hash"),
                ("RetryCount", 0),
                ("ResendCount", 0),
                ("Completed", true)
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/api/account-management/users/{userId}");

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();
        Connection.RowExists("Users", userId.ToString()).Should().BeTrue();
        var deletedAt = Connection.ExecuteScalar<string>("SELECT DeletedAt FROM Users WHERE Id = @id", [new { id = userId.ToString() }]);
        deletedAt.Should().NotBeNullOrEmpty();
        Connection.RowExists("EmailLogins", emailLoginId.ToString()).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteUser_WhenUserNeverConfirmedEmail_ShouldPermanentlyDeleteUser()
    {
        // Arrange
        var userId = UserId.NewId();
        Connection.Insert("Users", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", userId.ToString()),
                ("CreatedAt", TimeProvider.GetUtcNow().AddMinutes(-10)),
                ("ModifiedAt", null),
                ("DeletedAt", null),
                ("Email", Faker.Internet.UniqueEmail()),
                ("FirstName", null),
                ("LastName", null),
                ("Title", null),
                ("Role", nameof(UserRole.Member)),
                ("EmailConfirmed", false),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Locale", "en-US")
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/api/account-management/users/{userId}");

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();
        Connection.RowExists("Users", userId.ToString()).Should().BeFalse();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("UserPurged");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.reason"].Should().Be(nameof(UserPurgeReason.NeverActivated));
    }
}
