using System.Net;
using System.Text.Json;
using FluentAssertions;
using PlatformPlatform.AccountManagement.Database;
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
        var response = await AuthenticatedHttpClient.DeleteAsync($"/api/account-management/users/{unknownUserId}");

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
                ("CreatedAt", DateTime.UtcNow.AddMinutes(-10)),
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
        var response = await AuthenticatedHttpClient.DeleteAsync($"/api/account-management/users/{userId}");

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();
        Connection.RowExists("Users", userId.ToString()).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteUser_WhenDeletingOwnUSer_ShouldGetForbidden()
    {
        // Arrange
        var existingUserId = DatabaseSeeder.User1.Id;

        // Act
        var response = await AuthenticatedHttpClient.DeleteAsync($"/api/account-management/users/{existingUserId}");

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "You cannot delete yourself.");
    }
}
