using System.Net;
using System.Net.Http.Json;
using PlatformPlatform.Account.Database;
using PlatformPlatform.Account.Features.Users.Commands;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Validation;
using Xunit;

namespace PlatformPlatform.Account.Tests.Users;

public sealed class UpdateCurrentUserTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task UpdateCurrentUser_WhenValid_ShouldUpdateUser()
    {
        // Arrange
        var command = new UpdateCurrentUserCommand(
            Faker.Name.FirstName(),
            Faker.Name.LastName(),
            Faker.Name.JobTitle()
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync("/api/account/users/me", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();
    }

    [Fact]
    public async Task UpdateCurrentUser_WhenInvalid_ShouldReturnBadRequest()
    {
        // Arrange
        var command = new UpdateCurrentUserCommand
        (
            Faker.Random.String2(31),
            Faker.Random.String2(31),
            Faker.Random.String2(51)
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync("/api/account/users/me", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("firstName", "First name must be between 1 and 30 characters."),
            new ErrorDetail("lastName", "Last name must be between 1 and 30 characters."),
            new ErrorDetail("title", "Title must be no longer than 50 characters.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);
    }
}
