using FluentAssertions;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Users.Domain;
using PlatformPlatform.AccountManagement.Users.Queries;
using PlatformPlatform.SharedKernel.Tests;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Users;

public sealed class GetUsersTests : EndpointBaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task GetUsers_WhenSearchingBasedOnUserEmail_ShouldReturnUser()
    {
        // Arrange
        const string searchString = "willgate";

        // Act
        var response = await AuthenticatedHttpClient.GetAsync($"/api/account-management/users?search={searchString}");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var userResponse = await response.DeserializeResponse<GetUsersResponseDto>();
        userResponse.Should().NotBeNull();
        userResponse!.TotalCount.Should().Be(1);
        userResponse.Users.First().Email.Should().Be(DatabaseSeeder.User1ForSearching.Email);
    }

    [Fact]
    public async Task GetUsers_WhenSearchingBasedOnUserFirstName_ShouldReturnUser()
    {
        // Arrange
        const string searchString = "Will";

        // Act
        var response = await AuthenticatedHttpClient.GetAsync($"/api/account-management/users?search={searchString}");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var userResponse = await response.DeserializeResponse<GetUsersResponseDto>();
        userResponse.Should().NotBeNull();
        userResponse!.TotalCount.Should().Be(1);
        userResponse.Users.First().FirstName.Should().Be(DatabaseSeeder.User1ForSearching.FirstName);
    }

    [Fact]
    public async Task GetUsers_WhenSearchingBasedOnFullName_ShouldReturnUser()
    {
        // Arrange
        const string searchString = "William Henry Gates";

        // Act
        var response = await AuthenticatedHttpClient.GetAsync($"/api/account-management/users?search={searchString}");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var userResponse = await response.DeserializeResponse<GetUsersResponseDto>();
        userResponse.Should().NotBeNull();
        userResponse!.TotalCount.Should().Be(1);
        userResponse.Users.First().LastName.Should().Be(DatabaseSeeder.User1ForSearching.LastName);
    }

    [Fact]
    public async Task GetUsers_WhenSearchingBasedOnUserRole_ShouldReturnUser()
    {
        // Arrange
        // Act
        var response = await AuthenticatedHttpClient.GetAsync($"/api/account-management/users?userRole={UserRole.Member}");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var userResponse = await response.DeserializeResponse<GetUsersResponseDto>();
        userResponse.Should().NotBeNull();
        userResponse!.TotalCount.Should().Be(1);
        userResponse.Users.First().Role.Should().Be(DatabaseSeeder.User1ForSearching.Role);
    }

    [Fact]
    public async Task GetUsers_WhenSearchingWithSpecificOrdering_ShouldReturnOrderedUsers()
    {
        // Act
        var response = await AuthenticatedHttpClient.GetAsync($"/api/account-management/users?orderBy={SortableUserProperties.Role}");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var userResponse = await response.DeserializeResponse<GetUsersResponseDto>();
        userResponse.Should().NotBeNull();
        userResponse!.TotalCount.Should().Be(3);
        userResponse.Users.First().Role.Should().Be(UserRole.Member);
        userResponse.Users.Last().Role.Should().Be(UserRole.Owner);
    }
}
