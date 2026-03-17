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

public sealed class GetUsersTests : EndpointBaseTest<AccountDbContext>
{
    private const string Email = "willgates@email.com";
    private const string FirstName = "William Henry";
    private const string LastName = "Gates";
    private const UserRole UserRole = Features.Users.Domain.UserRole.Member;

    public GetUsersTests()
    {
        Connection.Insert("users", [
                ("tenant_id", DatabaseSeeder.Tenant1.Id.ToString()),
                ("id", UserId.NewId().ToString()),
                ("created_at", TimeProvider.GetUtcNow().AddMinutes(-10)),
                ("modified_at", null),
                ("email", Email),
                ("first_name", FirstName),
                ("last_name", LastName),
                ("title", "Philanthropist & Innovator"),
                ("role", UserRole.ToString()),
                ("email_confirmed", true),
                ("avatar", JsonSerializer.Serialize(new Avatar())),
                ("locale", "en-US"),
                ("external_identities", "[]")
            ]
        );
        Connection.Insert("users", [
                ("tenant_id", DatabaseSeeder.Tenant1.Id.ToString()),
                ("id", UserId.NewId().ToString()),
                ("created_at", TimeProvider.GetUtcNow().AddMinutes(-10)),
                ("modified_at", null),
                ("email", "ada@lovelace.com"),
                ("first_name", "Ada"),
                ("last_name", "Lovelace"),
                ("title", "Mathematician & Writer"),
                ("role", UserRole.ToString()),
                ("email_confirmed", true),
                ("avatar", JsonSerializer.Serialize(new Avatar())),
                ("locale", "en-US"),
                ("external_identities", "[]")
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
