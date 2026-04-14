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

public sealed class UpdateTeamMembersTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task UpdateTeamMembers_WhenOwnerAddsMembers_ShouldSucceed()
    {
        // Arrange
        var teamId = InsertTeam("Engineering");
        var command = new UpdateTeamMembersCommand([DatabaseSeeder.Tenant1Member.Id], []);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account/teams/{teamId}/members", command);

        // Assert
        response.EnsureSuccessStatusCode();
        Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM team_members WHERE team_id = @teamId AND user_id = @userId",
            [new { teamId, userId = DatabaseSeeder.Tenant1Member.Id.ToString() }]
        ).Should().Be(1);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("TeamMemberAdded");
    }

    [Fact]
    public async Task UpdateTeamMembers_WhenTeamAdminAddsMembers_ShouldSucceed()
    {
        // Arrange
        var teamId = InsertTeam("Engineering");
        InsertTeamMember(teamId, DatabaseSeeder.Tenant1Member.Id, TeamMemberRole.Admin);
        var newUserId = InsertUser("newuser@tenant-1.com");
        var command = new UpdateTeamMembersCommand([newUserId], []);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AuthenticatedMemberHttpClient.PutAsJsonAsync($"/api/account/teams/{teamId}/members", command);

        // Assert
        response.EnsureSuccessStatusCode();
        Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM team_members WHERE team_id = @teamId AND user_id = @userId",
            [new { teamId, userId = newUserId.ToString() }]
        ).Should().Be(1);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("TeamMemberAdded");
    }

    [Fact]
    public async Task UpdateTeamMembers_WhenRegularMemberTriesToAddMembers_ShouldReturnForbidden()
    {
        // Arrange
        var teamId = InsertTeam("Engineering");
        InsertTeamMember(teamId, DatabaseSeeder.Tenant1Member.Id, TeamMemberRole.Member);
        var command = new UpdateTeamMembersCommand([DatabaseSeeder.Tenant1Owner.Id], []);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AuthenticatedMemberHttpClient.PutAsJsonAsync($"/api/account/teams/{teamId}/members", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only team admins, tenant owners, or tenant admins can manage team members.");
        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateTeamMembers_WhenNonMemberTriesToAddMembers_ShouldReturnForbidden()
    {
        // Arrange
        var teamId = InsertTeam("Engineering");
        var command = new UpdateTeamMembersCommand([DatabaseSeeder.Tenant1Owner.Id], []);

        // Act
        var response = await AuthenticatedMemberHttpClient.PutAsJsonAsync($"/api/account/teams/{teamId}/members", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only team admins, tenant owners, or tenant admins can manage team members.");
    }

    [Fact]
    public async Task UpdateTeamMembers_WhenOwnerRemovesMembers_ShouldSucceed()
    {
        // Arrange
        var teamId = InsertTeam("Engineering");
        InsertTeamMember(teamId, DatabaseSeeder.Tenant1Member.Id, TeamMemberRole.Member);
        var command = new UpdateTeamMembersCommand([], [DatabaseSeeder.Tenant1Member.Id]);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account/teams/{teamId}/members", command);

        // Assert
        response.EnsureSuccessStatusCode();
        Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM team_members WHERE team_id = @teamId AND user_id = @userId",
            [new { teamId, userId = DatabaseSeeder.Tenant1Member.Id.ToString() }]
        ).Should().Be(0);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("TeamMemberRemoved");
    }

    [Fact]
    public async Task UpdateTeamMembers_WhenAddingNonExistentUser_ShouldReturnNotFound()
    {
        // Arrange
        var teamId = InsertTeam("Engineering");
        var nonExistentUserId = UserId.NewId();
        var command = new UpdateTeamMembersCommand([nonExistentUserId], []);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account/teams/{teamId}/members", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"User with id '{nonExistentUserId}' not found.");
    }

    [Fact]
    public async Task UpdateTeamMembers_WhenTeamDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentTeamId = TeamId.NewId();
        var command = new UpdateTeamMembersCommand([DatabaseSeeder.Tenant1Member.Id], []);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account/teams/{nonExistentTeamId}/members", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"Team with id '{nonExistentTeamId}' not found.");
    }

    [Fact]
    public async Task UpdateTeamMembers_WhenAddingExistingMember_ShouldNotDuplicate()
    {
        // Arrange
        var teamId = InsertTeam("Engineering");
        InsertTeamMember(teamId, DatabaseSeeder.Tenant1Member.Id, TeamMemberRole.Member);
        var command = new UpdateTeamMembersCommand([DatabaseSeeder.Tenant1Member.Id], []);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account/teams/{teamId}/members", command);

        // Assert
        response.EnsureSuccessStatusCode();
        Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM team_members WHERE team_id = @teamId AND user_id = @userId",
            [new { teamId, userId = DatabaseSeeder.Tenant1Member.Id.ToString() }]
        ).Should().Be(1);

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateTeamMembers_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var teamId = InsertTeam("Engineering");
        var command = new UpdateTeamMembersCommand([DatabaseSeeder.Tenant1Member.Id], []);

        // Act
        var response = await AnonymousHttpClient.PutAsJsonAsync($"/api/account/teams/{teamId}/members", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateTeamMembers_WhenNewMemberAdded_ShouldDefaultToMemberRole()
    {
        // Arrange
        var teamId = InsertTeam("Engineering");
        var command = new UpdateTeamMembersCommand([DatabaseSeeder.Tenant1Member.Id], []);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account/teams/{teamId}/members", command);

        // Assert
        response.EnsureSuccessStatusCode();
        var role = Connection.ExecuteScalar<string>(
            "SELECT role FROM team_members WHERE team_id = @teamId AND user_id = @userId",
            [new { teamId, userId = DatabaseSeeder.Tenant1Member.Id.ToString() }]
        );
        role.Should().Be("Member");
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
