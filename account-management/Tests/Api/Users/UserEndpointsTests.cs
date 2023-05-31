using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.AccountManagement.Api.Users.Contracts;
using PlatformPlatform.AccountManagement.Domain.Users;
using PlatformPlatform.AccountManagement.Infrastructure;
using PlatformPlatform.AccountManagement.Tests.Infrastructure;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Api.Users;

public sealed class UserEndpointsTests : IDisposable
{
    // This string represents a custom DateTime format based on the built-in format "o".
    // The format "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'FFFFFFFK" is used to avoid trailing zeros in the DateTime string.
    // The 'F's in the format are upper-case to indicate that trailing zeros should be removed.
    // See https://stackoverflow.com/a/17349663
    private const string Iso8601TimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'FFFFFFFK";

    private readonly SqliteInMemoryDbContextFactory<AccountManagementDbContext> _sqliteInMemoryDbContextFactory;
    private readonly WebApplicationFactory<Program> _webApplicationFactory;

    public UserEndpointsTests()
    {
        _sqliteInMemoryDbContextFactory = new SqliteInMemoryDbContextFactory<AccountManagementDbContext>();

        _webApplicationFactory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the app's AccountManagementDbContext registration.
                var descriptor = services.Single(d =>
                    d.ServiceType == typeof(DbContextOptions<AccountManagementDbContext>));
                services.Remove(descriptor);

                // Add AccountManagementDbContext using SqLiteDbContextFactory
                services.AddScoped(_ => _sqliteInMemoryDbContextFactory.CreateContext());

                // Add DbContextOptions<AccountManagementDbContext> to the service collection.
                services.AddScoped(_ => _sqliteInMemoryDbContextFactory.CreateOptions());

                services.AddTransient<DatabaseSeeder>();
            });
        });

        var serviceScope = _webApplicationFactory.Services.CreateScope();
        serviceScope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    }

    public void Dispose()
    {
        _sqliteInMemoryDbContextFactory.Dispose();
    }

    [Fact]
    public async Task CreateUser_WhenValid_ShouldCreateUser()
    {
        // Arrange
        var startId = UserId.NewId(); // NewId will always generate an id that are greater than the previous one
        var httpClient = _webApplicationFactory.CreateClient();

        // Act
        var response = await httpClient.PostAsJsonAsync("/api/users/v1",
            new CreateUserRequest("test@test.com", UserRole.TenantUser)
        );

        // Assert
        response.EnsureSuccessStatusCode();

        var userDto = await response.Content.ReadFromJsonAsync<UserResponseDto>();
        var userId = (UserId) userDto!.Id;
        userId.Should().BeGreaterThan(startId, "We expect a valid User Id greater than the start Id");

        var createdAt = userDto.CreatedAt.ToString(Iso8601TimeFormat);
        var expectedBody =
            $@"{{""id"":""{userDto.Id}"",""createdAt"":""{createdAt}"",""modifiedAt"":null,""email"":""test@test.com"",""userRole"":0}}";

        var responseAsRawString = await response.Content.ReadAsStringAsync();
        responseAsRawString.Should().Be(expectedBody);

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        response.Headers.Location!.ToString().Should().Be($"/api/users/v1/{userId}");
    }

    [Fact]
    public async Task CreateUser_WhenInValid_ShouldNotCreateUser()
    {
        // Arrange
        var httpClient = _webApplicationFactory.CreateClient();

        // Act
        var response = await httpClient.PostAsJsonAsync("/api/users/v1",
            new CreateUserRequest("a", UserRole.TenantOwner)
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        const string expectedBody =
            """{"type":"https://httpstatuses.com/400","title":"Bad Request","status":400,"Errors":[{"code":"Email","message":"'Email' is not a valid email address."}]}""";
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Be(expectedBody);

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        response.Headers.Location.Should().BeNull();
    }

    [Fact]
    public async Task GetUser_WhenUserExists_ShouldReturnUser()
    {
        // Arrange
        var httpClient = _webApplicationFactory.CreateClient();
        var userId = DatabaseSeeder.User1Id;

        // Act
        var response = await httpClient.GetAsync($"/api/users/v1/{userId}");

        // Assert
        response.EnsureSuccessStatusCode();

        var userDto = await response.Content.ReadFromJsonAsync<UserResponseDto>();
        const string userEmail = DatabaseSeeder.User1Email;
        var createdAt = userDto?.CreatedAt.ToString(Iso8601TimeFormat);

        var expectedBody =
            $@"{{""id"":""{userId}"",""createdAt"":""{createdAt}"",""modifiedAt"":null,""email"":""{userEmail}"",""userRole"":0}}";
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Be(expectedBody);

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        response.Headers.Location.Should().BeNull();
    }

    [Fact]
    public async Task GetUser_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistingUserId = new UserId(999);

        var httpClient = _webApplicationFactory.CreateClient();

        // Act
        var response = await httpClient.GetAsync($"/api/users/v1/{nonExistingUserId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        const string expectedBody =
            """{"type":"https://httpstatuses.com/404","title":"Not Found","status":404,"detail":"User with id '999' not found."}""";
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Be(expectedBody);

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        response.Headers.Location.Should().BeNull();
    }

    [Fact]
    public async Task UpdateUser_WhenValid_ShouldUpdateUser()
    {
        // Arrange
        var httpClient = _webApplicationFactory.CreateClient();

        var userId = DatabaseSeeder.User1Id;

        // Act
        var response = await httpClient.PutAsJsonAsync($"/api/users/v1/{userId}",
            new UpdateUserRequest("updated@test.com", UserRole.TenantOwner)
        );

        // Assert
        response.EnsureSuccessStatusCode();

        var userDto = await response.Content.ReadFromJsonAsync<UserResponseDto>();
        userDto!.Email.Should().Be("updated@test.com");
        userDto.UserRole.Should().Be(UserRole.TenantOwner);

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        response.Headers.Location.Should().BeNull();
    }

    [Fact]
    public async Task UpdateUser_WhenInValid_ShouldReturnBadRequest()
    {
        // Arrange
        var httpClient = _webApplicationFactory.CreateClient();

        var userId = DatabaseSeeder.User1Id;

        // Act
        var response = await httpClient.PutAsJsonAsync($"/api/users/v1/{userId}",
            new UpdateUserRequest("Invalid Email", UserRole.TenantAdmin)
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        response.Headers.Location.Should().BeNull();
    }

    [Fact]
    public async Task UpdateUser_WhenUserDoesNotExists_ShouldReturnNotFound()
    {
        // Arrange
        var httpClient = _webApplicationFactory.CreateClient();
        const string nonExistingUserId = "999";

        // Act
        var response = await httpClient.PutAsJsonAsync($"/api/users/v1/{nonExistingUserId}",
            new UpdateUserRequest("updated@test.com", UserRole.TenantAdmin)
        );

        //Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        const string expectedBody =
            """{"type":"https://httpstatuses.com/404","title":"Not Found","status":404,"detail":"User with id '999' not found."}""";
        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Be(expectedBody);

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        response.Headers.Location.Should().BeNull();
    }

    [Fact]
    public async Task DeleteUser_WhenUserDoesNotExists_ShouldReturnNotFound()
    {
        // Arrange
        var httpClient = _webApplicationFactory.CreateClient();
        const string nonExistingUserId = "999";

        // Act
        var response = await httpClient.DeleteAsync($"/api/users/v1/{nonExistingUserId}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        // const string expectedBody = $@"{{""message"":""User with id '{nonExistingUserId}' not found.""}}";
        const string expectedBody =
            $@"{{""type"":""https://httpstatuses.com/404"",""title"":""Not Found"",""status"":404,""detail"":""User with id '{nonExistingUserId}' not found.""}}";

        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Be(expectedBody);

        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        response.Headers.Location.Should().BeNull();
    }

    [Fact]
    public async Task DeleteUser_WhenUserExists_ShouldDeleteUser()
    {
        // Arrange
        var httpClient = _webApplicationFactory.CreateClient();
        var userId = DatabaseSeeder.User1Id;

        // Act
        var response = await httpClient.DeleteAsync($"/api/users/v1/{userId}");

        // Assert
        response.EnsureSuccessStatusCode();

        response.Content.Headers.ContentType.Should().BeNull();
        response.Headers.Location.Should().BeNull();

        // Verify that is deleted
        var getResponse = await httpClient.GetAsync($"/api/users/v1/{userId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}