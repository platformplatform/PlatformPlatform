using FluentAssertions;
using NJsonSchema;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.SharedKernel.Tests;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Users;

public sealed class GetCurrentUserTests : EndpointBaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task GetLoggedInUser_WhenUserExists_ShouldReturnUserWithValidContract()
    {
        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account-management/users/me");

        // Assert
        response.ShouldBeSuccessfulGetRequest();

        var schema = await JsonSchema.FromJsonAsync(
            """
            {
                'type': 'object',
                'properties': {
                    'id': {'type': 'string', 'pattern': '^usr_[A-Z0-9]{26}$'},
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
}
