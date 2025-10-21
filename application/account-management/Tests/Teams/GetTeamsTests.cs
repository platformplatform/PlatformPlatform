using System.Net;
using FluentAssertions;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.Teams.Domain;
using PlatformPlatform.AccountManagement.Features.Teams.Queries;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Teams;

public sealed class GetTeamsTests : EndpointBaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task GetTeams_WhenTeamsExist_ShouldReturnAllTeamsForTenant()
    {
        // Arrange
        var team1Id = TeamId.NewId();
        var team2Id = TeamId.NewId();
        Connection.Insert("Teams", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", team1Id.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow()),
                ("ModifiedAt", null),
                ("Name", "Engineering"),
                ("Description", "Engineering team")
            ]
        );
        Connection.Insert("Teams", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", team2Id.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow()),
                ("ModifiedAt", null),
                ("Name", "Marketing"),
                ("Description", "Marketing team")
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account-management/teams");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var teamsResponse = await response.DeserializeResponse<TeamsResponse>();
        teamsResponse.Should().NotBeNull();
        teamsResponse.Teams.Should().HaveCount(2);
        teamsResponse.Teams[0].Id.Should().Be(team1Id);
        teamsResponse.Teams[1].Id.Should().Be(team2Id);
        teamsResponse.Teams[0].MemberCount.Should().Be(0);
    }

    [Fact]
    public async Task GetTeams_WhenNoTeamsExist_ShouldReturnEmptyArray()
    {
        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account-management/teams");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var teamsResponse = await response.DeserializeResponse<TeamsResponse>();
        teamsResponse.Should().NotBeNull();
        teamsResponse.Teams.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTeams_WhenTeamsFromDifferentTenantsExist_ShouldOnlyReturnCurrentTenantTeams()
    {
        // Arrange
        var tenant1TeamId = TeamId.NewId();
        var tenant2TeamId = TeamId.NewId();
        Connection.Insert("Teams", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", tenant1TeamId.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow()),
                ("ModifiedAt", null),
                ("Name", "Tenant 1 Team"),
                ("Description", "Team for tenant 1")
            ]
        );
        Connection.Insert("Teams", [
                ("TenantId", DatabaseSeeder.Tenant2.Id.ToString()),
                ("Id", tenant2TeamId.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow()),
                ("ModifiedAt", null),
                ("Name", "Tenant 2 Team"),
                ("Description", "Team for tenant 2")
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account-management/teams");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var teamsResponse = await response.DeserializeResponse<TeamsResponse>();
        teamsResponse.Should().NotBeNull();
        teamsResponse.Teams.Should().HaveCount(1);
        teamsResponse.Teams[0].Id.Should().Be(tenant1TeamId);
    }

    [Fact]
    public async Task GetTeams_ShouldReturnTeamsSortedByNameAscending()
    {
        // Arrange
        var team1Id = TeamId.NewId();
        var team2Id = TeamId.NewId();
        var team3Id = TeamId.NewId();
        Connection.Insert("Teams", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", team1Id.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow()),
                ("ModifiedAt", null),
                ("Name", "Zebra Team"),
                ("Description", "Last alphabetically")
            ]
        );
        Connection.Insert("Teams", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", team2Id.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow()),
                ("ModifiedAt", null),
                ("Name", "Alpha Team"),
                ("Description", "First alphabetically")
            ]
        );
        Connection.Insert("Teams", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", team3Id.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow()),
                ("ModifiedAt", null),
                ("Name", "Beta Team"),
                ("Description", "Second alphabetically")
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account-management/teams");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var teamsResponse = await response.DeserializeResponse<TeamsResponse>();
        teamsResponse.Should().NotBeNull();
        teamsResponse.Teams.Should().HaveCount(3);
        teamsResponse.Teams[0].Name.Should().Be("Alpha Team");
        teamsResponse.Teams[1].Name.Should().Be("Beta Team");
        teamsResponse.Teams[2].Name.Should().Be("Zebra Team");
    }

    [Fact]
    public async Task GetTeams_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await AnonymousHttpClient.GetAsync("/api/account-management/teams");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
