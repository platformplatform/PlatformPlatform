using System.Net;
using System.Net.Http.Json;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Users.Commands;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Validation;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Users;

public sealed class UpdateUserTests : EndpointBaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task UpdateUser_WhenValid_ShouldUpdateUser()
    {
        // Arrange
        var existingUserId = DatabaseSeeder.User1.Id;
        var command = new UpdateUserCommand
        {
            Email = Faker.Internet.Email(),
            FirstName = Faker.Name.FirstName(),
            LastName = Faker.Name.LastName(),
            Title = Faker.Name.JobTitle()
        };

        // Act
        var response = await AuthenticatedHttpClient.PutAsJsonAsync($"/api/account-management/users/{existingUserId}", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();
    }

    [Fact]
    public async Task UpdateUser_WhenInvalid_ShouldReturnBadRequest()
    {
        // Arrange
        var existingUserId = DatabaseSeeder.User1.Id;
        var command = new UpdateUserCommand
        {
            Email = Faker.InvalidEmail(),
            FirstName = Faker.Random.String(31),
            LastName = Faker.Random.String(31),
            Title = Faker.Random.String(51)
        };

        // Act
        var response = await AuthenticatedHttpClient.PutAsJsonAsync($"/api/account-management/users/{existingUserId}", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("Email", "Email must be in a valid format and no longer than 100 characters."),
            new ErrorDetail("FirstName", "First name must be no longer than 30 characters."),
            new ErrorDetail("LastName", "Last name must be no longer than 30 characters."),
            new ErrorDetail("Title", "Title must be no longer than 50 characters.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);
    }

    [Fact]
    public async Task UpdateUser_WhenUserDoesNotExists_ShouldReturnNotFound()
    {
        // Arrange
        var unknownUserId = UserId.NewId();
        var command = new UpdateUserCommand
        {
            Email = Faker.Internet.Email(),
            FirstName = Faker.Name.FirstName(),
            LastName = Faker.Name.LastName(),
            Title = Faker.Name.JobTitle()
        };

        // Act
        var response = await AuthenticatedHttpClient.PutAsJsonAsync($"/api/account-management/users/{unknownUserId}", command);

        //Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"User with id '{unknownUserId}' not found.");
    }
}
