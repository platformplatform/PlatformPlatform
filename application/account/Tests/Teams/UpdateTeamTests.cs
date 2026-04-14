using System.Net;
using System.Net.Http.Json;
using Account.Database;
using Account.Features.Teams.Commands;
using Account.Features.Teams.Domain;
using Account.Features.Teams.Shared;
using FluentAssertions;
using SharedKernel.Tests;
using SharedKernel.Tests.Persistence;
using SharedKernel.Validation;
using Xunit;

namespace Account.Tests.Teams;

public sealed class UpdateTeamTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task UpdateTeam_WhenValid_ShouldUpdateTeam()
    {
        // Arrange
        var teamId = InsertTeam("Engineering", "Old description");
        var command = new UpdateTeamCommand("Product Engineering", "New description");

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account/teams/{teamId}", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var teamResponse = await response.Content.ReadFromJsonAsync<TeamResponse>();
        teamResponse.Should().NotBeNull();
        teamResponse.Name.Should().Be("Product Engineering");
        teamResponse.Description.Should().Be("New description");

        var updatedName = Connection.ExecuteScalar<string>(
            "SELECT name FROM teams WHERE id = @id", [new { id = teamId }]
        );
        updatedName.Should().Be("Product Engineering");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("TeamUpdated");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateTeam_WhenTeamNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = TeamId.NewId();
        var command = new UpdateTeamCommand("Engineering", "Description");

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account/teams/{nonExistentId}", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"Team with id '{nonExistentId}' not found.");

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateTeam_WhenMember_ShouldReturnForbidden()
    {
        // Arrange
        var teamId = InsertTeam("Engineering");
        var command = new UpdateTeamCommand("Product Engineering", null);

        // Act
        var response = await AuthenticatedMemberHttpClient.PutAsJsonAsync($"/api/account/teams/{teamId}", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only owners and admins can update teams.");

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateTeam_WhenNameConflictsWithAnotherTeam_ShouldReturnBadRequest()
    {
        // Arrange
        InsertTeam("Sales");
        var engineeringTeamId = InsertTeam("Engineering");
        var command = new UpdateTeamCommand("Sales", null);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account/teams/{engineeringTeamId}", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, "A team with the name 'Sales' already exists.");

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateTeam_WhenRenamingToSameName_ShouldSucceed()
    {
        // Arrange
        var teamId = InsertTeam("Engineering", "Old description");
        var command = new UpdateTeamCommand("Engineering", "New description");

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account/teams/{teamId}", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedDescription = Connection.ExecuteScalar<string>(
            "SELECT description FROM teams WHERE id = @id", [new { id = teamId }]
        );
        updatedDescription.Should().Be("New description");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
    }

    [Fact]
    public async Task UpdateTeam_WhenNameIsEmpty_ShouldReturnBadRequest()
    {
        // Arrange
        var teamId = InsertTeam("Engineering");
        var command = new UpdateTeamCommand("", null);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account/teams/{teamId}", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("name", "Name must be between 1 and 50 characters.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);
    }

    [Fact]
    public async Task UpdateTeam_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var teamId = InsertTeam("Engineering");
        var command = new UpdateTeamCommand("Product Engineering", null);

        // Act
        var response = await AnonymousHttpClient.PutAsJsonAsync($"/api/account/teams/{teamId}", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private string InsertTeam(string name, string? description = null)
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
