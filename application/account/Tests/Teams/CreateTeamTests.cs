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

public sealed class CreateTeamTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task CreateTeam_WhenValid_ShouldCreateTeam()
    {
        // Arrange
        var command = new CreateTeamCommand("Engineering", "The engineering team");

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account/teams", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var teamResponse = await response.Content.ReadFromJsonAsync<TeamResponse>();
        teamResponse.Should().NotBeNull();
        teamResponse.Name.Should().Be("Engineering");
        teamResponse.Description.Should().Be("The engineering team");

        Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM teams WHERE tenant_id = @tenantId AND name = @name",
            [new { tenantId = DatabaseSeeder.Tenant1.Id.ToString(), name = "Engineering" }]
        ).Should().Be(1);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("TeamCreated");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task CreateTeam_WhenDescriptionIsNull_ShouldCreateTeam()
    {
        // Arrange
        var command = new CreateTeamCommand("Sales", null);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account/teams", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var teamResponse = await response.Content.ReadFromJsonAsync<TeamResponse>();
        teamResponse.Should().NotBeNull();
        teamResponse.Name.Should().Be("Sales");
        teamResponse.Description.Should().BeNull();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("TeamCreated");
    }

    [Fact]
    public async Task CreateTeam_WhenMember_ShouldReturnForbidden()
    {
        // Arrange
        var command = new CreateTeamCommand("Engineering", "The engineering team");

        // Act
        var response = await AuthenticatedMemberHttpClient.PostAsJsonAsync("/api/account/teams", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only owners and admins can create teams.");

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task CreateTeam_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var command = new CreateTeamCommand("Engineering", "The engineering team");

        // Act
        var response = await AnonymousHttpClient.PostAsJsonAsync("/api/account/teams", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateTeam_WhenNameAlreadyExists_ShouldReturnBadRequest()
    {
        // Arrange
        InsertTeam("Engineering");
        var command = new CreateTeamCommand("Engineering", "The engineering team");

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account/teams", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, "A team with the name 'Engineering' already exists.");

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateTeam_WhenNameIsCaseInsensitiveDuplicate_ShouldReturnBadRequest()
    {
        // Arrange
        InsertTeam("Engineering");
        var command = new CreateTeamCommand("ENGINEERING", "Another engineering team");

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account/teams", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, "A team with the name 'ENGINEERING' already exists.");

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateTeam_WhenNameIsEmpty_ShouldReturnBadRequest()
    {
        // Arrange
        var command = new CreateTeamCommand("", null);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account/teams", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("name", "Name must be between 1 and 50 characters.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);
    }

    [Fact]
    public async Task CreateTeam_WhenNameIsTooLong_ShouldReturnBadRequest()
    {
        // Arrange
        var command = new CreateTeamCommand(new string('x', 51), null);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account/teams", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("name", "Name must be between 1 and 50 characters.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);
    }

    [Fact]
    public async Task CreateTeam_WhenDescriptionIsTooLong_ShouldReturnBadRequest()
    {
        // Arrange
        var command = new CreateTeamCommand("Engineering", new string('x', 256));

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account/teams", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("description", "Description must be at most 255 characters.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);
    }

    [Fact]
    public async Task CreateTeam_WhenNameHasSurroundingWhitespace_ShouldTrimName()
    {
        // Arrange
        var command = new CreateTeamCommand("  Engineering  ", "  Desc  ");

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account/teams", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var teamResponse = await response.Content.ReadFromJsonAsync<TeamResponse>();
        teamResponse.Should().NotBeNull();
        teamResponse.Name.Should().Be("Engineering");
        teamResponse.Description.Should().Be("Desc");
    }

    private void InsertTeam(string name, string? description = null)
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
    }
}
