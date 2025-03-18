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
        var command = new UpdateCurrentUserCommand(
            Faker.Internet.Email(),
            Faker.Name.FirstName(),
            Faker.Name.LastName(),
            Faker.Name.JobTitle()
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync("/api/account-management/users/me", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();
    }

    [Fact]
    public async Task UpdateCurrentUser_WhenInvalid_ShouldReturnBadRequest()
    {
        // Arrange
        var command = new UpdateCurrentUserCommand
        (
            Faker.InvalidEmail(),
            Faker.Random.String(31),
            Faker.Random.String(31),
            Faker.Random.String(51)
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync("/api/account-management/users/me", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("email", "Email must be in a valid format and no longer than 100 characters."),
            new ErrorDetail("firstName", "First name must be no longer than 30 characters."),
            new ErrorDetail("lastName", "Last name must be no longer than 30 characters."),
            new ErrorDetail("title", "Title must be no longer than 50 characters.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);
    }
}
