using System.Net;
using FluentAssertions;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.Teams.Domain;
using PlatformPlatform.AccountManagement.Features.Teams.Queries;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Teams;

public sealed class GetTeamTests : EndpointBaseTest<AccountManagementDbContext>
{
    private readonly TeamId _teamId = TeamId.NewId();

    public GetTeamTests()
    {
        Connection.Insert("Teams", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", _teamId.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow().AddMinutes(-10)),
                ("ModifiedAt", null),
                ("Name", Faker.Company.CompanyName()),
                ("Description", Faker.Company.CatchPhrase())
            ]
        );
    }

    [Fact]
    public async Task GetTeam_WhenTeamExists_ShouldReturnTeamResponse()
    {
        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/api/account-management/teams/{_teamId}");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var teamResponse = await response.DeserializeResponse<TeamResponse>();
        teamResponse.Should().NotBeNull();
        teamResponse.Id.Should().Be(_teamId);
        teamResponse.TenantId.Should().Be(DatabaseSeeder.Tenant1.Id);
    }

    [Fact]
    public async Task GetTeam_WhenTeamDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentTeamId = TeamId.NewId();

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/api/account-management/teams/{nonExistentTeamId}");

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"Team with ID '{nonExistentTeamId}' not found.");
    }

    [Fact]
    public async Task GetTeam_WhenTeamFromDifferentTenant_ShouldReturnNotFound()
    {
        // Arrange
        var otherTenantTeamId = TeamId.NewId();
        Connection.Insert("Teams", [
                ("TenantId", DatabaseSeeder.Tenant2.Id.ToString()),
                ("Id", otherTenantTeamId.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow()),
                ("ModifiedAt", null),
                ("Name", Faker.Company.CompanyName()),
                ("Description", Faker.Company.CatchPhrase())
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/api/account-management/teams/{otherTenantTeamId}");

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"Team with ID '{otherTenantTeamId}' not found.");
    }

    [Fact]
    public async Task GetTeam_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await AnonymousHttpClient.GetAsync($"/api/account-management/teams/{_teamId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
