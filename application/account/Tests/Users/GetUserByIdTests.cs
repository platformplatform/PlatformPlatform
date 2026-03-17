using System.Net;
using System.Text.Json;
using Account.Database;
using Account.Features.Users.Domain;
using Account.Features.Users.Queries;
using FluentAssertions;
using SharedKernel.Domain;
using SharedKernel.Tests;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Users;

public sealed class GetUserByIdTests : EndpointBaseTest<AccountDbContext>
{
    private readonly UserId _userId = UserId.NewId();

    public GetUserByIdTests()
    {
        Connection.Insert("users", [
                ("tenant_id", DatabaseSeeder.Tenant1.Id.ToString()),
                ("id", _userId.ToString()),
                ("created_at", TimeProvider.GetUtcNow().AddMinutes(-10)),
                ("modified_at", null),
                ("email", Faker.Internet.UniqueEmail()),
                ("first_name", Faker.Name.FirstName()),
                ("last_name", Faker.Name.LastName()),
                ("title", Faker.Name.JobTitle()),
                ("role", nameof(UserRole.Member)),
                ("email_confirmed", true),
                ("avatar", JsonSerializer.Serialize(new Avatar())),
                ("locale", "en-US"),
                ("external_identities", "[]")
            ]
        );
    }

    [Fact]
    public async Task GetUserById_WhenUserExists_ShouldReturnUserDetails()
    {
        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/api/account/users/{_userId}");

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
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/api/account/users/{nonExistentUserId}");

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"User with ID '{nonExistentUserId}' not found.");
    }

    [Fact]
    public async Task GetUserById_WhenMemberTriesToAccessOtherUser_ShouldSucceed()
    {
        // Act
        var response = await AuthenticatedMemberHttpClient.GetAsync($"/api/account/users/{_userId}");

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
        var response = await AnonymousHttpClient.GetAsync($"/api/account/users/{_userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
