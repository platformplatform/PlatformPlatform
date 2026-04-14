using System.Net;
using Account.Database;
using Account.Features.Teams.Domain;
using FluentAssertions;
using SharedKernel.Tests;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Teams;

public sealed class DeleteTeamTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task DeleteTeam_WhenValid_ShouldDeleteTeam()
    {
        // Arrange
        var teamId = InsertTeam("Engineering");

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/api/account/teams/{teamId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        Connection.RowExists("teams", teamId).Should().BeFalse();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("TeamDeleted");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteTeam_WhenTeamNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentId = TeamId.NewId();

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/api/account/teams/{nonExistentId}");

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"Team with id '{nonExistentId}' not found.");

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteTeam_WhenMember_ShouldReturnForbidden()
    {
        // Arrange
        var teamId = InsertTeam("Engineering");

        // Act
        var response = await AuthenticatedMemberHttpClient.DeleteAsync($"/api/account/teams/{teamId}");

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only owners and admins can delete teams.");

        Connection.RowExists("teams", teamId).Should().BeTrue();
        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteTeam_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var teamId = InsertTeam("Engineering");

        // Act
        var response = await AnonymousHttpClient.DeleteAsync($"/api/account/teams/{teamId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        Connection.RowExists("teams", teamId).Should().BeTrue();
    }

    private string InsertTeam(string name)
    {
        var teamId = TeamId.NewId().ToString();
        Connection.Insert("teams", [
                ("tenant_id", DatabaseSeeder.Tenant1.Id.ToString()),
                ("id", teamId),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("name", name),
                ("description", null)
            ]
        );
        return teamId;
    }
}
