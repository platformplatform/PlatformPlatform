using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Account.Database;
using Account.Features.EmailAuthentication.Domain;
using Account.Features.Users.Domain;
using Account.Features.Users.Shared;
using FluentAssertions;
using SharedKernel.Domain;
using SharedKernel.Tests;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Users;

public sealed class DeleteUserTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task DeleteUser_WhenUserDoesNotExists_ShouldReturnNotFound()
    {
        // Arrange
        var unknownUserId = UserId.NewId();

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/api/account/users/{unknownUserId}");

        //Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"User with id '{unknownUserId}' not found.");
    }

    [Fact]
    public async Task DeleteUser_WhenUserExists_ShouldSoftDeleteUser()
    {
        // Arrange
        var userId = UserId.NewId();
        Connection.Insert("users", [
                ("tenant_id", DatabaseSeeder.Tenant1.Id.ToString()),
                ("id", userId.ToString()),
                ("created_at", TimeProvider.GetUtcNow().AddMinutes(-10)),
                ("modified_at", null),
                ("deleted_at", null),
                ("email", Faker.Internet.UniqueEmail()),
                ("first_name", Faker.Person.FirstName),
                ("last_name", Faker.Person.LastName),
                ("title", "Philanthropist & Innovator"),
                ("role", nameof(UserRole.Member)),
                ("email_confirmed", true),
                ("avatar", JsonSerializer.Serialize(new Avatar())),
                ("locale", "en-US"),
                ("external_identities", "[]"),
                ("rollout_bucket", 42)
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/api/account/users/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userResponse = await response.Content.ReadFromJsonAsync<UserResponse>();
        userResponse.Should().NotBeNull();
        userResponse.Id.Should().Be(userId);
        Connection.RowExists("users", userId.ToString()).Should().BeTrue();
        var deletedAt = Connection.ExecuteScalar<string>("SELECT deleted_at FROM users WHERE id = @id", [new { id = userId.ToString() }]);
        deletedAt.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task DeleteUser_WhenDeletingOwnUSer_ShouldGetForbidden()
    {
        // Arrange
        var existingUserId = DatabaseSeeder.Tenant1Owner.Id;

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/api/account/users/{existingUserId}");

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "You cannot delete yourself.");
    }

    [Fact]
    public async Task DeleteUser_WhenUserHasEmailLoginHistory_ShouldSoftDeleteUserAndKeepEmailLogins()
    {
        // Arrange
        var userId = UserId.NewId();
        Connection.Insert("users", [
                ("tenant_id", DatabaseSeeder.Tenant1.Id.ToString()),
                ("id", userId.ToString()),
                ("created_at", TimeProvider.GetUtcNow().AddMinutes(-10)),
                ("modified_at", null),
                ("deleted_at", null),
                ("email", Faker.Internet.UniqueEmail()),
                ("first_name", Faker.Person.FirstName),
                ("last_name", Faker.Person.LastName),
                ("title", "Philanthropist & Innovator"),
                ("role", nameof(UserRole.Member)),
                ("email_confirmed", true),
                ("avatar", JsonSerializer.Serialize(new Avatar())),
                ("locale", "en-US"),
                ("external_identities", "[]"),
                ("rollout_bucket", 42)
            ]
        );

        var email = Connection.ExecuteScalar<string>("SELECT email FROM users WHERE id = @id", [new { id = userId.ToString() }]);
        var emailLoginId = EmailLoginId.NewId();
        Connection.Insert("email_logins", [
                ("id", emailLoginId.ToString()),
                ("created_at", TimeProvider.GetUtcNow().AddMinutes(-5)),
                ("modified_at", null),
                ("email", email),
                ("type", nameof(EmailLoginType.Login)),
                ("one_time_password_hash", "hash"),
                ("retry_count", 0),
                ("resend_count", 0),
                ("completed", true)
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/api/account/users/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userResponse = await response.Content.ReadFromJsonAsync<UserResponse>();
        userResponse.Should().NotBeNull();
        userResponse.Id.Should().Be(userId);
        Connection.RowExists("users", userId.ToString()).Should().BeTrue();
        var deletedAt = Connection.ExecuteScalar<string>("SELECT deleted_at FROM users WHERE id = @id", [new { id = userId.ToString() }]);
        deletedAt.Should().NotBeNullOrEmpty();
        Connection.RowExists("email_logins", emailLoginId.ToString()).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteUser_WhenUserNeverConfirmedEmail_ShouldSoftDeleteUser()
    {
        // Arrange
        var userId = UserId.NewId();
        Connection.Insert("users", [
                ("tenant_id", DatabaseSeeder.Tenant1.Id.ToString()),
                ("id", userId.ToString()),
                ("created_at", TimeProvider.GetUtcNow().AddMinutes(-10)),
                ("modified_at", null),
                ("deleted_at", null),
                ("email", Faker.Internet.UniqueEmail()),
                ("first_name", null),
                ("last_name", null),
                ("title", null),
                ("role", nameof(UserRole.Member)),
                ("email_confirmed", false),
                ("avatar", JsonSerializer.Serialize(new Avatar())),
                ("locale", "en-US"),
                ("external_identities", "[]"),
                ("rollout_bucket", 42)
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/api/account/users/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userResponse = await response.Content.ReadFromJsonAsync<UserResponse>();
        userResponse.Should().NotBeNull();
        userResponse.Id.Should().Be(userId);
        Connection.RowExists("users", userId.ToString()).Should().BeTrue();
        var deletedAt = Connection.ExecuteScalar<string>("SELECT deleted_at FROM users WHERE id = @id", [new { id = userId.ToString() }]);
        deletedAt.Should().NotBeNullOrEmpty();
    }
}
