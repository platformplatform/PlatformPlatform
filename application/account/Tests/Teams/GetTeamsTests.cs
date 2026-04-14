using System.Net;
using System.Net.Http.Json;
using Account.Database;
using Account.Features.Teams.Domain;
using Account.Features.Teams.Queries;
using FluentAssertions;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Teams;

public sealed class GetTeamsTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task GetTeams_WhenNoTeams_ShouldReturnEmptyList()
    {
        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account/teams");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var teamsResponse = await response.Content.ReadFromJsonAsync<GetTeamsResponse>();
        teamsResponse.Should().NotBeNull();
        teamsResponse.Teams.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTeams_WhenTeamsExist_ShouldReturnTeamsOrderedByName()
    {
        // Arrange
        InsertTeam("Sales");
        InsertTeam("Engineering");
        InsertTeam("Marketing");

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account/teams");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var teamsResponse = await response.Content.ReadFromJsonAsync<GetTeamsResponse>();
        teamsResponse.Should().NotBeNull();
        teamsResponse.Teams.Should().HaveCount(3);
        teamsResponse.Teams.Select(t => t.Name).Should().Equal("Engineering", "Marketing", "Sales");
    }

    [Fact]
    public async Task GetTeams_WhenMember_ShouldReturnTeams()
    {
        // Arrange
        InsertTeam("Engineering");

        // Act
        var response = await AuthenticatedMemberHttpClient.GetAsync("/api/account/teams");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var teamsResponse = await response.Content.ReadFromJsonAsync<GetTeamsResponse>();
        teamsResponse.Should().NotBeNull();
        teamsResponse.Teams.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetTeams_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await AnonymousHttpClient.GetAsync("/api/account/teams");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private void InsertTeam(string name)
    {
        Connection.Insert("teams", [
                ("tenant_id", DatabaseSeeder.Tenant1.Id.ToString()),
                ("id", TeamId.NewId().ToString()),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("name", name),
                ("description", null)
            ]
        );
    }
}
