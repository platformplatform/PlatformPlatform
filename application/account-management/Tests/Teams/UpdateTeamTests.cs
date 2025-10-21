using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.Teams.Commands;
using PlatformPlatform.AccountManagement.Features.Teams.Domain;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using PlatformPlatform.SharedKernel.Validation;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Teams;

public sealed class UpdateTeamTests : EndpointBaseTest<AccountManagementDbContext>
{
    private readonly TeamId _teamId = TeamId.NewId();

    public UpdateTeamTests()
    {
        Connection.Insert("Teams", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", _teamId.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow()),
                ("ModifiedAt", null),
                ("Name", "Original Team Name"),
                ("Description", "Original description")
            ]
        );
    }

    [Fact]
    public async Task UpdateTeam_WhenValid_ShouldUpdateTeam()
    {
        // Arrange
        var command = new UpdateTeamCommand("Updated Team Name", "Updated description")
        {
            Id = _teamId
        };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account-management/teams/{_teamId}", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("TeamUpdated");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();

        var updatedName = Connection.ExecuteScalar<string>("SELECT Name FROM Teams WHERE Id = @id", new { id = _teamId.ToString() });
        updatedName.Should().Be("Updated Team Name");
    }

    [Fact]
    public async Task UpdateTeam_WhenNameIsEmpty_ShouldReturnBadRequest()
    {
        // Arrange
        var command = new UpdateTeamCommand(string.Empty, "Some description")
        {
            Id = _teamId
        };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account-management/teams/{_teamId}", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("Name", "Name must be between 1 and 100 characters.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);
    }

    [Fact]
    public async Task UpdateTeam_WhenNameIsTooLong_ShouldReturnBadRequest()
    {
        // Arrange
        var command = new UpdateTeamCommand(new string('a', 101), "Some description")
        {
            Id = _teamId
        };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account-management/teams/{_teamId}", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("Name", "Name must be between 1 and 100 characters.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);
    }

    [Fact]
    public async Task UpdateTeam_WhenDescriptionIsTooLong_ShouldReturnBadRequest()
    {
        // Arrange
        var command = new UpdateTeamCommand("Valid name", new string('a', 501))
        {
            Id = _teamId
        };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account-management/teams/{_teamId}", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("Description", "Description must be at most 500 characters.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);
    }

    [Fact]
    public async Task UpdateTeam_WhenDuplicateName_ShouldReturnConflict()
    {
        // Arrange
        var otherTeamId = TeamId.NewId();
        Connection.Insert("Teams", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", otherTeamId.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow()),
                ("ModifiedAt", null),
                ("Name", "Existing Team"),
                ("Description", "Existing description")
            ]
        );

        var command = new UpdateTeamCommand("Existing Team", "Some description")
        {
            Id = _teamId
        };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account-management/teams/{_teamId}", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Conflict, "A team with the name 'Existing Team' already exists.");
    }

    [Fact]
    public async Task UpdateTeam_WhenTeamDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentTeamId = TeamId.NewId();
        var command = new UpdateTeamCommand("Some name", "Some description")
        {
            Id = nonExistentTeamId
        };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account-management/teams/{nonExistentTeamId}", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"Team with ID '{nonExistentTeamId}' not found.");
    }

    [Fact]
    public async Task UpdateTeam_WhenNonOwnerUser_ShouldReturnForbidden()
    {
        // Arrange
        var command = new UpdateTeamCommand("Updated name", "Updated description")
        {
            Id = _teamId
        };

        // Act
        var response = await AuthenticatedMemberHttpClient.PutAsJsonAsync($"/api/account-management/teams/{_teamId}", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only tenant owners can update teams.");
    }

    [Fact]
    public async Task UpdateTeam_WhenTeamFromDifferentTenant_ShouldReturnNotFound()
    {
        // Arrange
        var otherTenantTeamId = TeamId.NewId();
        Connection.Insert("Teams", [
                ("TenantId", DatabaseSeeder.Tenant2.Id.ToString()),
                ("Id", otherTenantTeamId.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow()),
                ("ModifiedAt", null),
                ("Name", "Other Tenant Team"),
                ("Description", "Other tenant description")
            ]
        );

        var command = new UpdateTeamCommand("Updated name", "Updated description")
        {
            Id = otherTenantTeamId
        };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account-management/teams/{otherTenantTeamId}", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"Team with ID '{otherTenantTeamId}' not found.");
    }
}
