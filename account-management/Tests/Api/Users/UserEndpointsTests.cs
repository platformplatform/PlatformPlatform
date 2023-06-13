using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PlatformPlatform.AccountManagement.Application.Users;
using PlatformPlatform.AccountManagement.Domain.Users;
using PlatformPlatform.AccountManagement.Infrastructure;
using PlatformPlatform.SharedKernel.ApplicationCore.Validation;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Api.Users;

public sealed class UserEndpointsTests : BaseApiTests<AccountManagementDbContext>
{
    [Fact]
    public async Task CreateUser_WhenValid_ShouldCreateUser()
    {
        // Act
        var command = new CreateUser.Command(DatabaseSeeder.Tenant1.Id, "test@test.com", UserRole.TenantUser);
        var response = await TestHttpClient.PostAsJsonAsync("/api/users", command);

        // Assert
        await EnsureSuccessPostRequest(response, startsWith: "/api/users/");
        response.Headers.Location!.ToString().Length.Should().Be($"/api/users/{UserId.NewId()}".Length);
    }

    [Fact]
    public async Task CreateUser_WhenInvalid_ShouldReturnBadRequest()
    {
        // Act
        var command = new CreateUser.Command(DatabaseSeeder.Tenant1.Id, "a", UserRole.TenantOwner);
        var response = await TestHttpClient.PostAsJsonAsync("/api/users", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("Email", "'Email' is not a valid email address.")
        };
        await EnsureErrorStatusCode(response, HttpStatusCode.BadRequest, expectedErrors);
    }

    [Fact]
    public async Task GetUser_WhenUserExists_ShouldReturnUserWithValidContract()
    {
        // Act
        var response = await TestHttpClient.GetAsync($"/api/users/{DatabaseSeeder.User1.Id}");

        // Assert
        EnsureSuccessGetRequest(response);

        var userDto = await response.Content.ReadFromJsonAsync<UserResponseDto>();
        var createdAt = userDto?.CreatedAt.ToString(Iso8601TimeFormat);
        var expectedBody =
            $$"""{"id":"{{DatabaseSeeder.User1.Id}}","createdAt":"{{createdAt}}","modifiedAt":null,"email":"{{DatabaseSeeder.User1.Email}}","userRole":0}""";
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Be(expectedBody);
    }

    [Fact]
    public async Task GetUser_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        // Act
        var response = await TestHttpClient.GetAsync("/api/users/999");

        // Assert
        await EnsureErrorStatusCode(response, HttpStatusCode.NotFound, "User with id '999' not found.");
    }

    [Fact]
    public async Task UpdateUser_WhenValid_ShouldUpdateUser()
    {
        // Act
        var command = new UpdateUser.Command {Email = "updated@test.com", UserRole = UserRole.TenantOwner};
        var response = await TestHttpClient.PutAsJsonAsync($"/api/users/{DatabaseSeeder.User1.Id}", command);

        // Assert
        EnsureSuccessPutRequest(response);
    }

    [Fact]
    public async Task UpdateUser_WhenInvalid_ShouldReturnBadRequest()
    {
        // Act
        var command = new UpdateUser.Command {Email = "Invalid Email", UserRole = UserRole.TenantAdmin};
        var response = await TestHttpClient.PutAsJsonAsync($"/api/users/{DatabaseSeeder.User1.Id}", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("Email", "'Email' is not a valid email address.")
        };
        await EnsureErrorStatusCode(response, HttpStatusCode.BadRequest, expectedErrors);
    }

    [Fact]
    public async Task UpdateUser_WhenUserDoesNotExists_ShouldReturnNotFound()
    {
        // Act
        var command = new UpdateUser.Command {Email = "updated@test.com", UserRole = UserRole.TenantAdmin};
        var response = await TestHttpClient.PutAsJsonAsync("/api/users/999", command);

        //Assert
        await EnsureErrorStatusCode(response, HttpStatusCode.NotFound, "User with id '999' not found.");
    }

    [Fact]
    public async Task DeleteUser_WhenUserDoesNotExists_ShouldReturnNotFound()
    {
        // Act
        var response = await TestHttpClient.DeleteAsync("/api/users/999");

        //Assert
        await EnsureErrorStatusCode(response, HttpStatusCode.NotFound, "User with id '999' not found.");
    }

    [Fact]
    public async Task DeleteUser_WhenUserExists_ShouldDeleteUser()
    {
        // Act
        var response = await TestHttpClient.DeleteAsync($"/api/users/{DatabaseSeeder.User1.Id}");

        // Assert
        EnsureSuccessDeleteRequest(response);

        // Verify that User is deleted
        Connection
            .ExecuteScalar("SELECT COUNT(*) FROM Users WHERE Id = @id", new {id = DatabaseSeeder.User1.Id.ToString()})
            .Should().Be(0);
    }
}