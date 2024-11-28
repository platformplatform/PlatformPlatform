using System.Net;
using FluentAssertions;
using NJsonSchema;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Tests;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Users;

public sealed class GetUserTests : EndpointBaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task GetUser_WhenUserExists_ShouldReturnUserWithValidContract()
    {
        // Arrange
        var existingUserId = DatabaseSeeder.User1.Id;

        // Act
        var response = await AuthenticatedHttpClient.GetAsync($"/api/account-management/users/{existingUserId}");

        // Assert
        response.ShouldBeSuccessfulGetRequest();

        var schema = await JsonSchema.FromJsonAsync(
            """
            {
                'type': 'object',
                'properties': {
                    'id': {'type': 'long'},
                    'createdAt': {'type': 'string', 'format': 'date-time'},
                    'modifiedAt': {'type': ['null', 'string'], 'format': 'date-time'},
                    'email': {'type': 'string', 'maxLength': 100},
                    'firstName': {'type': ['null', 'string'], 'maxLength': 30},
                    'lastName': {'type': ['null', 'string'], 'maxLength': 30},
                    'title': {'type': ['null', 'string'], 'maxLength': 50},
                    'role': {'type': 'string', 'minLength': 1, 'maxLength': 20},
                    'emailConfirmed': {'type': 'boolean'},
                    'avatarUrl': {'type': ['null', 'string'], 'maxLength': 100},
                },
                'required': ['id', 'createdAt', 'modifiedAt', 'email', 'role'],
                'additionalProperties': false
            }
            """
        );

        var responseBody = await response.Content.ReadAsStringAsync();
        schema.Validate(responseBody).Should().BeEmpty();
    }

    [Fact]
    public async Task GetUser_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var unknownUserId = UserId.NewId();

        // Act
        var response = await AuthenticatedHttpClient.GetAsync($"/api/account-management/users/{unknownUserId}");

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"User with id '{unknownUserId}' not found.");
    }

    [Fact]
    public async Task GetUser_WhenInvalidUserId_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidUserId = Faker.Random.AlphaNumeric(31);

        // Act
        var response = await AuthenticatedHttpClient.GetAsync($"/api/account-management/users/{invalidUserId}");

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, $"""Failed to bind parameter "UserId Id" from "{invalidUserId}".""");
    }
}
