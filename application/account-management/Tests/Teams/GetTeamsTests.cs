using System.Net;
using FluentAssertions;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.TeamMembers.Domain;
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

    [Fact]
    public async Task GetTeams_WhenTeamHasNoMembers_ShouldReturnMemberCountZero()
    {
        // Arrange
        var teamId = TeamId.NewId();
        Connection.Insert("Teams", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", teamId.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow()),
                ("ModifiedAt", null),
                ("Name", "Empty Team"),
                ("Description", "Team with no members")
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account-management/teams");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var teamsResponse = await response.DeserializeResponse<TeamsResponse>();
        teamsResponse.Should().NotBeNull();
        teamsResponse.Teams.Should().HaveCount(1);
        teamsResponse.Teams[0].Id.Should().Be(teamId);
        teamsResponse.Teams[0].MemberCount.Should().Be(0);
    }

    [Fact]
    public async Task GetTeams_WhenTeamHasMembers_ShouldReturnAccurateMemberCount()
    {
        // Arrange
        var teamId = TeamId.NewId();
        var userId1 = DatabaseSeeder.Tenant1Owner.Id;
        var userId2 = DatabaseSeeder.Tenant1Member.Id;
        Connection.Insert("Teams", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", teamId.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow()),
                ("ModifiedAt", null),
                ("Name", "Team with Members"),
                ("Description", "Team with multiple members")
            ]
        );
        var teamMemberId1 = TeamMemberId.NewId();
        var teamMemberId2 = TeamMemberId.NewId();
        Connection.Insert("TeamMembers", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", teamMemberId1.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow()),
                ("ModifiedAt", null),
                ("TeamId", teamId.ToString()),
                ("UserId", userId1.ToString()),
                ("Role", (int)TeamMemberRole.Admin)
            ]
        );
        Connection.Insert("TeamMembers", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", teamMemberId2.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow()),
                ("ModifiedAt", null),
                ("TeamId", teamId.ToString()),
                ("UserId", userId2.ToString()),
                ("Role", (int)TeamMemberRole.Member)
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account-management/teams");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var teamsResponse = await response.DeserializeResponse<TeamsResponse>();
        teamsResponse.Should().NotBeNull();
        teamsResponse.Teams.Should().HaveCount(1);
        teamsResponse.Teams[0].Id.Should().Be(teamId);
        teamsResponse.Teams[0].MemberCount.Should().Be(2);
    }

    [Fact]
    public async Task GetTeams_WithMultipleTeams_ShouldReturnCorrectMemberCountForEach()
    {
        // Arrange
        var team1Id = TeamId.NewId();
        var team2Id = TeamId.NewId();
        var team3Id = TeamId.NewId();
        var userId1 = DatabaseSeeder.Tenant1Owner.Id;
        var userId2 = DatabaseSeeder.Tenant1Member.Id;

        Connection.Insert("Teams", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", team1Id.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow()),
                ("ModifiedAt", null),
                ("Name", "Team A"),
                ("Description", "First team")
            ]
        );
        Connection.Insert("Teams", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", team2Id.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow()),
                ("ModifiedAt", null),
                ("Name", "Team B"),
                ("Description", "Second team")
            ]
        );
        Connection.Insert("Teams", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", team3Id.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow()),
                ("ModifiedAt", null),
                ("Name", "Team C"),
                ("Description", "Third team")
            ]
        );

        // Add members: Team1 has 2, Team2 has 1, Team3 has 0
        Connection.Insert("TeamMembers", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", TeamMemberId.NewId().ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow()),
                ("ModifiedAt", null),
                ("TeamId", team1Id.ToString()),
                ("UserId", userId1.ToString()),
                ("Role", (int)TeamMemberRole.Admin)
            ]
        );
        Connection.Insert("TeamMembers", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", TeamMemberId.NewId().ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow()),
                ("ModifiedAt", null),
                ("TeamId", team1Id.ToString()),
                ("UserId", userId2.ToString()),
                ("Role", (int)TeamMemberRole.Member)
            ]
        );
        Connection.Insert("TeamMembers", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", TeamMemberId.NewId().ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow()),
                ("ModifiedAt", null),
                ("TeamId", team2Id.ToString()),
                ("UserId", userId1.ToString()),
                ("Role", (int)TeamMemberRole.Admin)
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account-management/teams");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var teamsResponse = await response.DeserializeResponse<TeamsResponse>();
        teamsResponse.Should().NotBeNull();
        teamsResponse.Teams.Should().HaveCount(3);

        var teamA = teamsResponse.Teams.First(t => t.Name == "Team A");
        var teamB = teamsResponse.Teams.First(t => t.Name == "Team B");
        var teamC = teamsResponse.Teams.First(t => t.Name == "Team C");

        teamA.MemberCount.Should().Be(2);
        teamB.MemberCount.Should().Be(1);
        teamC.MemberCount.Should().Be(0);
    }
}
