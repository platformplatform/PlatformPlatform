using System.Net;
using System.Text.Json;
using FluentAssertions;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.Authentication.Domain;
using PlatformPlatform.AccountManagement.Features.EmailConfirmations.Domain;
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
    public async Task DeleteUser_WhenUserExists_ShouldDeleteUser()
    {
        // Arrange
        var userId = UserId.NewId();
        Connection.Insert("Users", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", userId.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow().AddMinutes(-10)),
                ("ModifiedAt", null),
                ("Email", Faker.Internet.Email()),
                ("FirstName", Faker.Person.FirstName),
                ("LastName", Faker.Person.LastName),
                ("Title", "Philanthropist & Innovator"),
                ("Role", UserRole.Member.ToString()),
                ("EmailConfirmed", true),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Locale", "en-US")
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/api/account-management/users/{userId}");

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();
        Connection.RowExists("Users", userId.ToString()).Should().BeFalse();
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
    public async Task DeleteUser_WhenUserHasLoginHistory_ShouldDeleteUserAndLogins()
    {
        // Arrange
        var userId = UserId.NewId();
        Connection.Insert("Users", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", userId.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow().AddMinutes(-10)),
                ("ModifiedAt", null),
                ("Email", Faker.Internet.Email()),
                ("FirstName", Faker.Person.FirstName),
                ("LastName", Faker.Person.LastName),
                ("Title", "Philanthropist & Innovator"),
                ("Role", UserRole.Member.ToString()),
                ("EmailConfirmed", true),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Locale", "en-US")
            ]
        );

        // Create a login record for the user
        var emailConfirmationId = EmailConfirmationId.NewId();
        var loginId = LoginId.NewId();
        Connection.Insert("Logins", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", loginId.ToString()),
                ("UserId", userId.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow().AddMinutes(-5)),
                ("ModifiedAt", null),
                ("EmailConfirmationId", emailConfirmationId.ToString()),
                ("Completed", true)
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/api/account-management/users/{userId}");

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();
        Connection.RowExists("Users", userId.ToString()).Should().BeFalse();
        Connection.RowExists("Logins", loginId.ToString()).Should().BeFalse();
    }
}
