using System.Net;
using FluentAssertions;
using PlatformPlatform.AccountManagement.Database;
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
        var existingUserId = DatabaseSeeder.User1.Id;

        // Act
        var response = await AuthenticatedHttpClient.DeleteAsync($"/api/account-management/users/{existingUserId}");

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();
        Connection.RowExists("Users", existingUserId.ToString()).Should().BeFalse();
    }
}
