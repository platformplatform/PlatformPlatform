using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using NJsonSchema;
using PlatformPlatform.AccountManagement.Application.Users;
using PlatformPlatform.AccountManagement.Infrastructure;
using PlatformPlatform.SharedKernel.ApplicationCore.Validation;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Api.Users;

public sealed class UserEndpointsTests : BaseApiTests<AccountManagementDbContext>
{
    [Fact]
    public async Task GetUser_WhenUserExists_ShouldReturnUserWithValidContract()
    {
        // Arrange
        var existingUserId = DatabaseSeeder.User1.Id;

        // Act
        var response = await TestHttpClient.GetAsync($"/api/users/{existingUserId}");

        // Assert
        EnsureSuccessGetRequest(response);

        var schema = await JsonSchema.FromJsonAsync(
            """
            {
                'type': 'object',
                'properties': {
                    'id': {'type': 'long'},
                    'createdAt': {'type': 'string', 'format': 'date-time'},
                    'modifiedAt': {'type': ['null', 'string'], 'format': 'date-time'},
                    'email': {'type': 'string', 'maxLength': 100},
                    'userRole': {'type': 'string', 'minLength': 1, 'maxLength':20}
                },
                'required': ['id', 'createdAt', 'modifiedAt', 'email', 'userRole'],
                'additionalProperties': false
            }
            """);

        var responseBody = await response.Content.ReadAsStringAsync();
        schema.Validate(responseBody).Should().BeEmpty();
    }

    [Fact]
    public async Task GetUser_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var unknownUserId = Faker.RandomUlid();

        // Act
        var response = await TestHttpClient.GetAsync($"/api/users/{unknownUserId}");

        // Assert
        await EnsureErrorStatusCode(response, HttpStatusCode.NotFound, $"User with id '{unknownUserId}' not found.");
    }

    [Fact]
    public async Task GetUser_WhenInvalidUserId_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidUserId = Faker.Random.AlphaNumeric(31);

        // Act
        var response = await TestHttpClient.GetAsync($"/api/users/{invalidUserId}");

        // Assert
        await EnsureErrorStatusCode(response, HttpStatusCode.BadRequest,
            $"""Failed to bind parameter "UserId id" from "{invalidUserId}".""");
    }

    [Fact]
    public async Task CreateUser_WhenValid_ShouldCreateUser()
    {
        // Arrange
        var existingTenantId = DatabaseSeeder.Tenant1.Id;
        var command = new CreateUser.Command(existingTenantId, Faker.Internet.Email(), UserRole.TenantUser);

        // Act
        var response = await TestHttpClient.PostAsJsonAsync("/api/users", command);

        // Assert
        await EnsureSuccessPostRequest(response, startsWith: "/api/users/");
        response.Headers.Location!.ToString().Length.Should().Be($"/api/users/{UserId.NewId()}".Length);
    }

    [Fact]
    public async Task CreateUser_WhenInvalidEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var existingTenantId = DatabaseSeeder.Tenant1.Id;
        var invalidEmail = Faker.InvalidEmail();
        var command = new CreateUser.Command(existingTenantId, invalidEmail, UserRole.TenantUser);

        // Act
        var response = await TestHttpClient.PostAsJsonAsync("/api/users", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("Email", "Email must be in a valid format and no longer than 100 characters.")
        };
        await EnsureErrorStatusCode(response, HttpStatusCode.BadRequest, expectedErrors);
    }

    [Fact]
    public async Task CreateUser_WhenUserExists_ShouldReturnBadRequest()
    {
        // Arrange
        var existingTenantId = DatabaseSeeder.Tenant1.Id;
        var existingUserEmail = DatabaseSeeder.User1.Email;
        var command = new CreateUser.Command(existingTenantId, existingUserEmail, UserRole.TenantUser);

        // Act
        var response = await TestHttpClient.PostAsJsonAsync("/api/users", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("Email",
                $"The email '{existingUserEmail}' is already in use by another user on this tenant.")
        };
        await EnsureErrorStatusCode(response, HttpStatusCode.BadRequest, expectedErrors);
    }

    [Fact]
    public async Task CreateUser_WhenTenantDoesNotExists_ShouldReturnBadRequest()
    {
        // Arrange
        var unknownTenantId = Faker.Subdomain();
        var command =
            new CreateUser.Command(new TenantId(unknownTenantId), Faker.Internet.Email(), UserRole.TenantUser);

        // Act
        var response = await TestHttpClient.PostAsJsonAsync("/api/users", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("TenantId", $"The tenant '{unknownTenantId}' does not exist.")
        };
        await EnsureErrorStatusCode(response, HttpStatusCode.BadRequest, expectedErrors);
    }

    [Fact]
    public async Task UpdateUser_WhenValid_ShouldUpdateUser()
    {
        // Arrange
        var existingUserId = DatabaseSeeder.User1.Id;
        var command = new UpdateUser.Command {Email = Faker.Internet.Email(), UserRole = UserRole.TenantOwner};

        // Act
        var response = await TestHttpClient.PutAsJsonAsync($"/api/users/{existingUserId}", command);

        // Assert
        EnsureSuccessWithEmptyHeaderAndLocation(response);
    }

    [Fact]
    public async Task UpdateUser_WhenInvalid_ShouldReturnBadRequest()
    {
        // Arrange
        var existingUserId = DatabaseSeeder.User1.Id;
        var invalidEmail = Faker.InvalidEmail();
        var command = new UpdateUser.Command {Email = invalidEmail, UserRole = UserRole.TenantAdmin};

        // Act
        var response = await TestHttpClient.PutAsJsonAsync($"/api/users/{existingUserId}", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("Email", "Email must be in a valid format and no longer than 100 characters.")
        };
        await EnsureErrorStatusCode(response, HttpStatusCode.BadRequest, expectedErrors);
    }

    [Fact]
    public async Task UpdateUser_WhenUserDoesNotExists_ShouldReturnNotFound()
    {
        // Arrange
        var unknownUserId = Faker.RandomUlid();
        var command = new UpdateUser.Command {Email = Faker.Internet.Email(), UserRole = UserRole.TenantAdmin};

        // Act
        var response = await TestHttpClient.PutAsJsonAsync($"/api/users/{unknownUserId}", command);

        //Assert
        await EnsureErrorStatusCode(response, HttpStatusCode.NotFound, $"User with id '{unknownUserId}' not found.");
    }

    [Fact]
    public async Task DeleteUser_WhenUserDoesNotExists_ShouldReturnNotFound()
    {
        // Arrange
        var unknownUserId = Faker.RandomUlid();

        // Act
        var response = await TestHttpClient.DeleteAsync($"/api/users/{unknownUserId}");

        //Assert
        await EnsureErrorStatusCode(response, HttpStatusCode.NotFound, $"User with id '{unknownUserId}' not found.");
    }

    [Fact]
    public async Task DeleteUser_WhenUserExists_ShouldDeleteUser()
    {
        // Arrange
        var existingUserId = DatabaseSeeder.User1.Id;

        // Act
        var response = await TestHttpClient.DeleteAsync($"/api/users/{existingUserId}");

        // Assert
        EnsureSuccessWithEmptyHeaderAndLocation(response);
        Connection.RowExists("Users", existingUserId.ToString()).Should().BeFalse();
    }
}