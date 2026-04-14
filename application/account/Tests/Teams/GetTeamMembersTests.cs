using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Account.Database;
using Account.Features.Teams.Domain;
using Account.Features.Teams.Queries;
using Account.Features.Users.Domain;
using FluentAssertions;
using SharedKernel.Domain;
using SharedKernel.Tests;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Teams;

public sealed class GetTeamMembersTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task GetTeamMembers_WhenOwnerRequestsTeamMembers_ShouldReturnAllMembers()
    {
        // Arrange
        var teamId = InsertTeam("Engineering");
        InsertTeamMember(teamId, DatabaseSeeder.Tenant1Member.Id, TeamMemberRole.Admin);
        var newUserId = InsertUser("newuser@tenant-1.com");
        InsertTeamMember(teamId, newUserId, TeamMemberRole.Member);

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/api/account/teams/{teamId}/members");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TeamMembersResponse>();
        result.Should().NotBeNull();
        result.Members.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTeamMembers_WhenTeamMemberRequestsMembers_ShouldReturnAllMembers()
    {
        // Arrange
        var teamId = InsertTeam("Engineering");
        InsertTeamMember(teamId, DatabaseSeeder.Tenant1Member.Id, TeamMemberRole.Member);

        // Act
        var response = await AuthenticatedMemberHttpClient.GetAsync($"/api/account/teams/{teamId}/members");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TeamMembersResponse>();
        result.Should().NotBeNull();
        result.Members.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetTeamMembers_WhenNonMemberRequestsMembers_ShouldReturnForbidden()
    {
        // Arrange
        var teamId = InsertTeam("Engineering");

        // Act
        var response = await AuthenticatedMemberHttpClient.GetAsync($"/api/account/teams/{teamId}/members");

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only team members, tenant owners, or tenant admins can view team members.");
    }

    [Fact]
    public async Task GetTeamMembers_WhenTeamDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentTeamId = TeamId.NewId();

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/api/account/teams/{nonExistentTeamId}/members");

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"Team with id '{nonExistentTeamId}' not found.");
    }

    [Fact]
    public async Task GetTeamMembers_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var teamId = InsertTeam("Engineering");

        // Act
        var response = await AnonymousHttpClient.GetAsync($"/api/account/teams/{teamId}/members");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTeamMembers_WhenTeamHasMultipleMembersWithDifferentRoles_ShouldReturnCorrectRoles()
    {
        // Arrange
        var teamId = InsertTeam("Engineering");
        InsertTeamMember(teamId, DatabaseSeeder.Tenant1Member.Id, TeamMemberRole.Admin);
        var memberId = InsertUser("member@tenant-1.com");
        InsertTeamMember(teamId, memberId, TeamMemberRole.Member);

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/api/account/teams/{teamId}/members");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TeamMembersResponse>();
        result.Should().NotBeNull();
        result.Members.Should().HaveCount(2);
        result.Members.Should().Contain(m => m.UserId == DatabaseSeeder.Tenant1Member.Id && m.Role == TeamMemberRole.Admin);
        result.Members.Should().Contain(m => m.UserId == memberId && m.Role == TeamMemberRole.Member);
    }

    [Fact]
    public async Task GetTeamMembers_WhenTeamHasNoMembers_ShouldReturnEmptyList()
    {
        // Arrange
        var teamId = InsertTeam("Engineering");

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/api/account/teams/{teamId}/members");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TeamMembersResponse>();
        result.Should().NotBeNull();
        result.Members.Should().BeEmpty();
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
