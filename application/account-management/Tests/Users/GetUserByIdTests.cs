using System.Net;
using System.Text.Json;
using FluentAssertions;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Queries;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Users;

public sealed class GetUserByIdTests : EndpointBaseTest<AccountManagementDbContext>
{
    private readonly UserId _userId = UserId.NewId();

    public GetUserByIdTests()
    {
        Connection.Insert("Users", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", _userId.ToString()),
                ("CreatedAt", TimeProvider.GetUtcNow().AddMinutes(-10)),
                ("ModifiedAt", null),
                ("Email", Faker.Internet.UniqueEmail()),
                ("FirstName", Faker.Name.FirstName()),
                ("LastName", Faker.Name.LastName()),
                ("Title", Faker.Name.JobTitle()),
                ("Role", nameof(UserRole.Member)),
                ("EmailConfirmed", true),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Locale", "en-US"),
                ("ExternalIdentities", "[]")
            ]
        );
    }

    [Fact]
    public async Task GetUserById_WhenUserExists_ShouldReturnUserDetails()
    {
        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/api/account-management/users/{_userId}");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var userDetails = await response.DeserializeResponse<UserDetails>();
        userDetails.Should().NotBeNull();
        userDetails.Id.Should().Be(_userId);
    }

    [Fact]
    public async Task GetUserById_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentUserId = UserId.NewId();

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/api/account-management/users/{nonExistentUserId}");

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"User with ID '{nonExistentUserId}' not found.");
    }

    [Fact]
    public async Task GetUserById_WhenMemberTriesToAccessOtherUser_ShouldSucceed()
    {
        // Act
        var response = await AuthenticatedMemberHttpClient.GetAsync($"/api/account-management/users/{_userId}");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var userDetails = await response.DeserializeResponse<UserDetails>();
        userDetails.Should().NotBeNull();
        userDetails.Id.Should().Be(_userId);
    }

    [Fact]
    public async Task GetUserById_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await AnonymousHttpClient.GetAsync($"/api/account-management/users/{_userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
