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

public sealed class GetUsersTests : EndpointBaseTest<AccountManagementDbContext>
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
                ("CreatedAt", TimeProvider.System.GetUtcNow().AddMinutes(-10)),
                ("ModifiedAt", null),
                ("Email", Email),
                ("FirstName", FirstName),
                ("LastName", LastName),
                ("Title", "Philanthropist & Innovator"),
                ("Role", UserRole.ToString()),
                ("EmailConfirmed", true),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Locale", "en-US")
            ]
        );
        Connection.Insert("Users", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", UserId.NewId().ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow().AddMinutes(-10)),
                ("ModifiedAt", null),
                ("Email", Faker.Internet.Email()),
                ("FirstName", Faker.Name.FirstName()),
                ("LastName", Faker.Name.LastName()),
                ("Title", Faker.Name.JobTitle()),
                ("Role", UserRole.ToString()),
                ("EmailConfirmed", true),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Locale", "en-US")
            ]
        );
    }

    [Fact]
    public async Task GetUsers_WhenSearchingBasedOnUserEmail_ShouldReturnUser()
    {
        // Arrange
        const string searchString = "willgate";

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/api/account-management/users?search={searchString}");

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
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/api/account-management/users?search={searchString}");

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
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/api/account-management/users?search={searchString}");

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
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/api/account-management/users?userRole={UserRole.Member}");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var userResponse = await response.DeserializeResponse<UsersResponse>();
        userResponse.Should().NotBeNull();
        userResponse.TotalCount.Should().Be(2);
        userResponse.Users.First().Role.Should().Be(UserRole);
    }

    [Fact]
    public async Task GetUsers_WhenSearchingWithSpecificOrdering_ShouldReturnOrderedUsers()
    {
        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/api/account-management/users?orderBy={SortableUserProperties.Role}");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var userResponse = await response.DeserializeResponse<UsersResponse>();
        userResponse.Should().NotBeNull();
        userResponse.TotalCount.Should().Be(3);
        userResponse.Users.First().Role.Should().Be(UserRole.Member);
        userResponse.Users.Last().Role.Should().Be(UserRole.Owner);
    }
}
