using System.Net;
using System.Net.Http.Json;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.Users.Commands;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Validation;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Users;

public sealed class UpdateCurrentUserTests : EndpointBaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task UpdateCurrentUser_WhenValid_ShouldUpdateUser()
    {
        // Arrange
        var command = new UpdateCurrentUserCommand
        {
            Email = Faker.Internet.Email(),
            FirstName = Faker.Name.FirstName(),
            LastName = Faker.Name.LastName(),
            Title = Faker.Name.JobTitle()
        };

        // Act
        var response = await AuthenticatedHttpClient.PutAsJsonAsync("/api/account-management/users/me", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();
    }

    [Fact]
    public async Task UpdateCurrentUser_WhenInvalid_ShouldReturnBadRequest()
    {
        // Arrange
        var command = new UpdateCurrentUserCommand
        {
            Email = Faker.InvalidEmail(),
            FirstName = Faker.Random.String(31),
            LastName = Faker.Random.String(31),
            Title = Faker.Random.String(51)
        };

        // Act
        var response = await AuthenticatedHttpClient.PutAsJsonAsync("/api/account-management/users/me", command);

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
}