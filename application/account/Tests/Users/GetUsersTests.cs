using System.Text.Json;
using FluentAssertions;
using PlatformPlatform.Account.Database;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.Account.Features.Users.Queries;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using Xunit;

namespace PlatformPlatform.Account.Tests.Users;

public sealed class GetUsersTests : EndpointBaseTest<AccountDbContext>
{
    private const string Email = "willgates@email.com";
    private const string FirstName = "William Henry";
    private const string LastName = "Gates";
    private const UserRole UserRole = Features.Users.Domain.UserRole.Member;

    public GetUsersTests()
    {
        Connection.Insert("Users", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", UserId.NewId().ToString()),
                ("CreatedAt", TimeProvider.GetUtcNow().AddMinutes(-10)),
                ("ModifiedAt", null),
                ("Email", Email),
                ("FirstName", FirstName),
                ("LastName", LastName),
                ("Title", "Philanthropist & Innovator"),
                ("Role", UserRole.ToString()),
                ("EmailConfirmed", true),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Locale", "en-US"),
                ("ExternalIdentities", "[]")
            ]
        );
        Connection.Insert("Users", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", UserId.NewId().ToString()),
                ("CreatedAt", TimeProvider.GetUtcNow().AddMinutes(-10)),
                ("ModifiedAt", null),
                ("Email", "ada@lovelace.com"),
                ("FirstName", "Ada"),
                ("LastName", "Lovelace"),
                ("Title", "Mathematician & Writer"),
                ("Role", UserRole.ToString()),
                ("EmailConfirmed", true),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Locale", "en-US"),
                ("ExternalIdentities", "[]")
            ]
        );
    }

    [Fact]
    public async Task GetUsers_WhenSearchingBasedOnUserEmail_ShouldReturnUser()
    {
        // Arrange
        const string searchString = "willgate";

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/api/account/users?search={searchString}");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var userResponse = await response.DeserializeResponse<UsersResponse>();
        userResponse.Should().NotBeNull();
        userResponse.TotalCount.Should().Be(1);
        userResponse.Users.First().Email.Should().Be(Email);
    }

    [Fact]
    public async Task GetUsers_WhenSearchingBasedOnUserFirstName_ShouldReturnUser()
    {
        // Arrange
        const string searchString = "Will";

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/api/account/users?search={searchString}");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var userResponse = await response.DeserializeResponse<UsersResponse>();
        userResponse.Should().NotBeNull();
        userResponse.TotalCount.Should().Be(1);
        userResponse.Users.First().FirstName.Should().Be(FirstName);
    }

    [Fact]
    public async Task GetUsers_WhenSearchingBasedOnFullName_ShouldReturnUser()
    {
        // Arrange
        const string searchString = "William Henry Gates";

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/api/account/users?search={searchString}");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var userResponse = await response.DeserializeResponse<UsersResponse>();
        userResponse.Should().NotBeNull();
        userResponse.TotalCount.Should().Be(1);
        userResponse.Users.First().LastName.Should().Be(LastName);
    }

    [Fact]
    public async Task GetUsers_WhenSearchingBasedOnUserRole_ShouldReturnUser()
    {
        // Arrange
        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/api/account/users?userRole={UserRole.Member}");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var userResponse = await response.DeserializeResponse<UsersResponse>();
        userResponse.Should().NotBeNull();
        userResponse.TotalCount.Should().Be(3);
        userResponse.Users.First().Role.Should().Be(UserRole);
    }

    [Fact]
    public async Task GetUsers_WhenSearchingWithSpecificOrdering_ShouldReturnOrderedUsers()
    {
        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/api/account/users?orderBy={SortableUserProperties.Role}");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var userResponse = await response.DeserializeResponse<UsersResponse>();
        userResponse.Should().NotBeNull();
        userResponse.TotalCount.Should().Be(4);
        userResponse.Users.First().Role.Should().Be(UserRole.Member);
        userResponse.Users.Last().Role.Should().Be(UserRole.Owner);
    }
}
