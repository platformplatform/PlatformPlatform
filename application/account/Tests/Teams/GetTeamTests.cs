using System.Net;
using System.Net.Http.Json;
using Account.Database;
using Account.Features.Teams.Domain;
using Account.Features.Teams.Shared;
using FluentAssertions;
using SharedKernel.Tests;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Teams;

public sealed class GetTeamTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task GetTeam_WhenTeamExists_ShouldReturnTeam()
    {
        // Arrange
        var teamId = InsertTeam("Engineering", "The engineering team");

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/api/account/teams/{teamId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var teamResponse = await response.Content.ReadFromJsonAsync<TeamResponse>();
        teamResponse.Should().NotBeNull();
        teamResponse.Id.Value.Should().Be(teamId);
        teamResponse.Name.Should().Be("Engineering");
        teamResponse.Description.Should().Be("The engineering team");
    }

    [Fact]
    public async Task GetTeam_WhenTeamNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = TeamId.NewId();

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/api/account/teams/{nonExistentId}");

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"Team with id '{nonExistentId}' not found.");
    }

    [Fact]
    public async Task GetTeam_WhenMember_ShouldReturnTeam()
    {
        // Arrange
        var teamId = InsertTeam("Engineering", null);

        // Act
        var response = await AuthenticatedMemberHttpClient.GetAsync($"/api/account/teams/{teamId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetTeam_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var teamId = InsertTeam("Engineering", null);

        // Act
        var response = await AnonymousHttpClient.GetAsync($"/api/account/teams/{teamId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private string InsertTeam(string name, string? description)
    {
        var teamId = TeamId.NewId().ToString();
        Connection.Insert("teams", [
                ("tenant_id", DatabaseSeeder.Tenant1.Id.ToString()),
                ("id", teamId),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("name", name),
                ("description", description)
            ]
        );
        return teamId;
    }
}
