using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.Users.Commands;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Validation;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Users;

public sealed class CreateUserTests : EndpointBaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task CreateUser_WhenValid_ShouldCreateUser()
    {
        // Arrange
        var command = new CreateUserCommand(Faker.Internet.Email(), UserRole.Member, false, null);

        // Act
        var response = await AuthenticatedHttpClient.PostAsJsonAsync("/api/account-management/users", command);

        // Assert
        await response.ShouldBeSuccessfulPostRequest(startsWith: "/api/account-management/users/");
        response.Headers.Location!.ToString().Length.Should().Be($"/api/account-management/users/{UserId.NewId()}".Length);
    }

    [Fact]
    public async Task CreateUser_WhenInvalidEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidEmail = Faker.InvalidEmail();
        var command = new CreateUserCommand(invalidEmail, UserRole.Member, false, null);

        // Act
        var response = await AuthenticatedHttpClient.PostAsJsonAsync("/api/account-management/users", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("Email", "Email must be in a valid format and no longer than 100 characters.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);
    }

    [Fact]
    public async Task CreateUser_WhenUserExists_ShouldReturnBadRequest()
    {
        // Arrange
        var existingUserEmail = DatabaseSeeder.User1.Email;
        var command = new CreateUserCommand(existingUserEmail, UserRole.Member, false, null);

        // Act
        var response = await AuthenticatedHttpClient.PostAsJsonAsync("/api/account-management/users", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("Email", $"The email '{existingUserEmail}' is already in use by another user on this tenant.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);
    }
}
