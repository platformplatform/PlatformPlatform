using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Account.Database;
using Account.Features.Teams.Commands;
using Account.Features.Teams.Domain;
using Account.Features.Users.Domain;
using FluentAssertions;
using SharedKernel.Domain;
using SharedKernel.Tests;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Teams;

public sealed class ChangeTeamMemberRoleTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task ChangeTeamMemberRole_WhenOwnerChangesRole_ShouldSucceed()
    {
        // Arrange
        var teamId = InsertTeam("Engineering");
        InsertTeamMember(teamId, DatabaseSeeder.Tenant1Member.Id, TeamMemberRole.Member);
        var command = new ChangeTeamMemberRoleCommand(DatabaseSeeder.Tenant1Member.Id, TeamMemberRole.Admin);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync(
            $"/api/account/teams/{teamId}/members/{DatabaseSeeder.Tenant1Member.Id}/role", command
        );

        // Assert
        response.EnsureSuccessStatusCode();
        var role = Connection.ExecuteScalar<string>(
            "SELECT role FROM team_members WHERE team_id = @teamId AND user_id = @userId",
            [new { teamId, userId = DatabaseSeeder.Tenant1Member.Id.ToString() }]
        );
        role.Should().Be("Admin");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("TeamMemberRoleChanged");
    }

    [Fact]
    public async Task ChangeTeamMemberRole_WhenTeamAdminTriesToChangeRole_ShouldReturnForbidden()
    {
        // Arrange
        var teamId = InsertTeam("Engineering");
        InsertTeamMember(teamId, DatabaseSeeder.Tenant1Member.Id, TeamMemberRole.Admin);
        var newUserId = InsertUser("newuser@tenant-1.com");
        InsertTeamMember(teamId, newUserId, TeamMemberRole.Member);
        var command = new ChangeTeamMemberRoleCommand(newUserId, TeamMemberRole.Admin);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AuthenticatedMemberHttpClient.PutAsJsonAsync(
            $"/api/account/teams/{teamId}/members/{newUserId}/role", command
        );

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only tenant owners and tenant admins can change team member roles.");
        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task ChangeTeamMemberRole_WhenRegularMemberTriesToChangeRole_ShouldReturnForbidden()
    {
        // Arrange
        var teamId = InsertTeam("Engineering");
        InsertTeamMember(teamId, DatabaseSeeder.Tenant1Member.Id, TeamMemberRole.Member);
        var command = new ChangeTeamMemberRoleCommand(DatabaseSeeder.Tenant1Member.Id, TeamMemberRole.Admin);

        // Act
        var response = await AuthenticatedMemberHttpClient.PutAsJsonAsync(
            $"/api/account/teams/{teamId}/members/{DatabaseSeeder.Tenant1Member.Id}/role", command
        );

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only tenant owners and tenant admins can change team member roles.");
    }

    [Fact]
    public async Task ChangeTeamMemberRole_WhenUserIsNotTeamMember_ShouldReturnNotFound()
    {
        // Arrange
        var teamId = InsertTeam("Engineering");
        var command = new ChangeTeamMemberRoleCommand(DatabaseSeeder.Tenant1Member.Id, TeamMemberRole.Admin);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync(
            $"/api/account/teams/{teamId}/members/{DatabaseSeeder.Tenant1Member.Id}/role", command
        );

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"User with id '{DatabaseSeeder.Tenant1Member.Id}' is not a member of team '{teamId}'.");
    }

    [Fact]
    public async Task ChangeTeamMemberRole_WhenTeamDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentTeamId = TeamId.NewId();
        var command = new ChangeTeamMemberRoleCommand(DatabaseSeeder.Tenant1Member.Id, TeamMemberRole.Admin);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync(
            $"/api/account/teams/{nonExistentTeamId}/members/{DatabaseSeeder.Tenant1Member.Id}/role", command
        );

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"Team with id '{nonExistentTeamId}' not found.");
    }

    [Fact]
    public async Task ChangeTeamMemberRole_WhenRoleIsSame_ShouldSucceedWithoutEvent()
    {
        // Arrange
        var teamId = InsertTeam("Engineering");
        InsertTeamMember(teamId, DatabaseSeeder.Tenant1Member.Id, TeamMemberRole.Admin);
        var command = new ChangeTeamMemberRoleCommand(DatabaseSeeder.Tenant1Member.Id, TeamMemberRole.Admin);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync(
            $"/api/account/teams/{teamId}/members/{DatabaseSeeder.Tenant1Member.Id}/role", command
        );

        // Assert
        response.EnsureSuccessStatusCode();
        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task ChangeTeamMemberRole_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var teamId = InsertTeam("Engineering");
        InsertTeamMember(teamId, DatabaseSeeder.Tenant1Member.Id, TeamMemberRole.Member);
        var command = new ChangeTeamMemberRoleCommand(DatabaseSeeder.Tenant1Member.Id, TeamMemberRole.Admin);

        // Act
        var response = await AnonymousHttpClient.PutAsJsonAsync(
            $"/api/account/teams/{teamId}/members/{DatabaseSeeder.Tenant1Member.Id}/role", command
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ChangeTeamMemberRole_WhenDemotingFromAdmin_ShouldSucceed()
    {
        // Arrange
        var teamId = InsertTeam("Engineering");
        InsertTeamMember(teamId, DatabaseSeeder.Tenant1Member.Id, TeamMemberRole.Admin);
        var command = new ChangeTeamMemberRoleCommand(DatabaseSeeder.Tenant1Member.Id, TeamMemberRole.Member);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync(
            $"/api/account/teams/{teamId}/members/{DatabaseSeeder.Tenant1Member.Id}/role", command
        );

        // Assert
        response.EnsureSuccessStatusCode();
        var role = Connection.ExecuteScalar<string>(
            "SELECT role FROM team_members WHERE team_id = @teamId AND user_id = @userId",
            [new { teamId, userId = DatabaseSeeder.Tenant1Member.Id.ToString() }]
        );
        role.Should().Be("Member");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("TeamMemberRoleChanged");
    }

    private string InsertTeam(string name)
    {
        var teamId = TeamId.NewId().ToString();
        Connection.Insert("teams", [
                ("tenant_id", DatabaseSeeder.Tenant1.Id.ToString()),
                ("id", teamId),
                ("created_at", TimeProvider.GetUtcNow().AddMinutes(-10)),
                ("modified_at", null),
                ("name", name),
                ("description", $"{name} team")
            ]
        );
        return teamId;
    }

    private void InsertTeamMember(string teamId, UserId userId, TeamMemberRole role)
    {
        var teamMemberId = TeamMemberId.NewId().ToString();
        Connection.Insert("team_members", [
                ("tenant_id", DatabaseSeeder.Tenant1.Id.ToString()),
                ("id", teamMemberId),
                ("team_id", teamId),
                ("user_id", userId.ToString()),
                ("created_at", TimeProvider.GetUtcNow().AddMinutes(-5)),
                ("modified_at", null),
                ("role", role.ToString())
            ]
        );
    }

    private UserId InsertUser(string email)
    {
        var userId = UserId.NewId();
        Connection.Insert("users", [
                ("tenant_id", DatabaseSeeder.Tenant1.Id.ToString()),
                ("id", userId.ToString()),
                ("created_at", TimeProvider.GetUtcNow().AddMinutes(-10)),
                ("modified_at", null),
                ("email", email),
                ("email_confirmed", true),
                ("first_name", "Test"),
                ("last_name", "User"),
                ("title", null),
                ("avatar", JsonSerializer.Serialize(new Avatar())),
                ("role", nameof(UserRole.Member)),
                ("locale", "en-US"),
                ("external_identities", "[]"),
                ("rollout_bucket", 42)
            ]
        );
        return userId;
    }
}
