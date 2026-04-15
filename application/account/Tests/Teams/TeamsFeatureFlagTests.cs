using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Account.Database;
using Account.Features.Teams.Commands;
using Account.Features.Teams.Domain;
using SharedKernel.Tests;
using Xunit;

namespace Account.Tests.Teams;

public sealed class TeamsFeatureFlagTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task GetTeams_WhenTeamsFlagDisabled_ShouldReturnNotFound()
    {
        // Arrange
        var client = CreateOwnerClientWithoutTeamsFlag();

        // Act
        var response = await client.GetAsync("/api/account/teams");

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, "Teams feature is not enabled for this tenant.");
    }

    [Fact]
    public async Task GetTeam_WhenTeamsFlagDisabled_ShouldReturnNotFound()
    {
        // Arrange
        var client = CreateOwnerClientWithoutTeamsFlag();
        var teamId = TeamId.NewId();

        // Act
        var response = await client.GetAsync($"/api/account/teams/{teamId}");

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, "Teams feature is not enabled for this tenant.");
    }

    [Fact]
    public async Task CreateTeam_WhenTeamsFlagDisabled_ShouldReturnNotFound()
    {
        // Arrange
        var client = CreateOwnerClientWithoutTeamsFlag();
        var command = new CreateTeamCommand("Engineering", "Description");

        // Act
        var response = await client.PostAsJsonAsync("/api/account/teams", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, "Teams feature is not enabled for this tenant.");
    }

    [Fact]
    public async Task UpdateTeam_WhenTeamsFlagDisabled_ShouldReturnNotFound()
    {
        // Arrange
        var client = CreateOwnerClientWithoutTeamsFlag();
        var teamId = TeamId.NewId();
        var command = new UpdateTeamCommand("Engineering", null);

        // Act
        var response = await client.PutAsJsonAsync($"/api/account/teams/{teamId}", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, "Teams feature is not enabled for this tenant.");
    }

    [Fact]
    public async Task DeleteTeam_WhenTeamsFlagDisabled_ShouldReturnNotFound()
    {
        // Arrange
        var client = CreateOwnerClientWithoutTeamsFlag();
        var teamId = TeamId.NewId();

        // Act
        var response = await client.DeleteAsync($"/api/account/teams/{teamId}");

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, "Teams feature is not enabled for this tenant.");
    }

    [Fact]
    public async Task GetTeamMembers_WhenTeamsFlagDisabled_ShouldReturnNotFound()
    {
        // Arrange
        var client = CreateOwnerClientWithoutTeamsFlag();
        var teamId = TeamId.NewId();

        // Act
        var response = await client.GetAsync($"/api/account/teams/{teamId}/members");

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, "Teams feature is not enabled for this tenant.");
    }

    [Fact]
    public async Task UpdateTeamMembers_WhenTeamsFlagDisabled_ShouldReturnNotFound()
    {
        // Arrange
        var client = CreateOwnerClientWithoutTeamsFlag();
        var teamId = TeamId.NewId();
        var command = new UpdateTeamMembersCommand([], []);

        // Act
        var response = await client.PutAsJsonAsync($"/api/account/teams/{teamId}/members", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, "Teams feature is not enabled for this tenant.");
    }

    [Fact]
    public async Task ChangeTeamMemberRole_WhenTeamsFlagDisabled_ShouldReturnNotFound()
    {
        // Arrange
        var client = CreateOwnerClientWithoutTeamsFlag();
        var teamId = TeamId.NewId();
        var command = new ChangeTeamMemberRoleCommand(DatabaseSeeder.Tenant1Member.Id, TeamMemberRole.Admin);

        // Act
        var response = await client.PutAsJsonAsync(
            $"/api/account/teams/{teamId}/members/{DatabaseSeeder.Tenant1Member.Id}/role", command
        );

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, "Teams feature is not enabled for this tenant.");
    }

    private HttpClient CreateOwnerClientWithoutTeamsFlag()
    {
        var ownerUserInfo = CreateUserInfo(DatabaseSeeder.Tenant1Owner, DatabaseSeeder.Tenant1OwnerSession.Id, []);
        var ownerAccessToken = AccessTokenGenerator.Generate(ownerUserInfo);
        var client = WebApplicationFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerAccessToken);
        return client;
    }
}
