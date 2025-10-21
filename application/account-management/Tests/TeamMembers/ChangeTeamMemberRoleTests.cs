using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.TeamMembers.Commands;
using PlatformPlatform.AccountManagement.Features.TeamMembers.Domain;
using PlatformPlatform.AccountManagement.Features.Teams.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.TeamMembers;

public sealed class ChangeTeamMemberRoleTests : EndpointBaseTest<AccountManagementDbContext>
{
    private readonly UserId _adminUserId = UserId.NewId();
    private readonly UserId _memberUserId = UserId.NewId();
    private readonly UserId _nonMemberUserId = UserId.NewId();
    private readonly TeamId _teamId = TeamId.NewId();

    public ChangeTeamMemberRoleTests()
    {
        // Create a team
        Connection.Insert("Teams", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", _teamId.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow().AddMinutes(-30)),
                ("ModifiedAt", null),
                ("Name", "Test Team"),
                ("Description", "Test description")
            ]
        );

        // Create users
        Connection.Insert("Users", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", _adminUserId.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow().AddMinutes(-20)),
                ("ModifiedAt", null),
                ("Email", "admin@test.com"),
                ("FirstName", "Admin"),
                ("LastName", "User"),
                ("Title", "Admin"),
                ("Role", nameof(UserRole.Member)),
                ("EmailConfirmed", true),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Locale", "en-US")
            ]
        );

        Connection.Insert("Users", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", _memberUserId.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow().AddMinutes(-15)),
                ("ModifiedAt", null),
                ("Email", "member@test.com"),
                ("FirstName", "Member"),
                ("LastName", "User"),
                ("Title", "Member"),
                ("Role", nameof(UserRole.Member)),
                ("EmailConfirmed", true),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Locale", "en-US")
            ]
        );

        Connection.Insert("Users", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", _nonMemberUserId.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow().AddMinutes(-10)),
                ("ModifiedAt", null),
                ("Email", "nonmember@test.com"),
                ("FirstName", "Non"),
                ("LastName", "Member"),
                ("Title", "User"),
                ("Role", nameof(UserRole.Member)),
                ("EmailConfirmed", true),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Locale", "en-US")
            ]
        );

        // Create team admin
        var adminMemberId = TeamMemberId.NewId();
        Connection.Insert("TeamMembers", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", adminMemberId.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow().AddMinutes(-15)),
                ("ModifiedAt", null),
                ("TeamId", _teamId.ToString()),
                ("UserId", _adminUserId.ToString()),
                ("Role", nameof(TeamMemberRole.Admin))
            ]
        );

        // Create regular member
        var memberMemberId = TeamMemberId.NewId();
        Connection.Insert("TeamMembers", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", memberMemberId.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow().AddMinutes(-10)),
                ("ModifiedAt", null),
                ("TeamId", _teamId.ToString()),
                ("UserId", _memberUserId.ToString()),
                ("Role", nameof(TeamMemberRole.Member))
            ]
        );
    }

    [Fact]
    public async Task ChangeTeamMemberRole_WhenTeamAdminPromotesMemberToAdmin_ShouldSucceed()
    {
        // Arrange
        var command = new ChangeTeamMemberRoleCommand(TeamMemberRole.Admin);
        var adminClient = CreateAuthenticatedHttpClient(DatabaseSeeder.Tenant1.Id, _adminUserId, UserRole.Member);

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/account-management/teams/{_teamId}/members/{_memberUserId}/role", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();
        Connection.ExecuteScalar<string>("SELECT Role FROM TeamMembers WHERE TeamId = @teamId AND UserId = @userId",
            new { teamId = _teamId.ToString(), userId = _memberUserId.ToString() }
        ).Should().Be(nameof(TeamMemberRole.Admin));

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("TeamMemberRoleChanged");
    }

    [Fact]
    public async Task ChangeTeamMemberRole_WhenTeamAdminDemotesOtherAdminToMember_ShouldSucceed()
    {
        // Arrange - create another admin
        var otherAdminUserId = UserId.NewId();
        Connection.Insert("Users", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", otherAdminUserId.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow()),
                ("ModifiedAt", null),
                ("Email", "otheradmin@test.com"),
                ("FirstName", "Other"),
                ("LastName", "Admin"),
                ("Title", "Admin"),
                ("Role", nameof(UserRole.Member)),
                ("EmailConfirmed", true),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Locale", "en-US")
            ]
        );

        var otherAdminMemberId = TeamMemberId.NewId();
        Connection.Insert("TeamMembers", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", otherAdminMemberId.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow()),
                ("ModifiedAt", null),
                ("TeamId", _teamId.ToString()),
                ("UserId", otherAdminUserId.ToString()),
                ("Role", nameof(TeamMemberRole.Admin))
            ]
        );

        var command = new ChangeTeamMemberRoleCommand(TeamMemberRole.Member);
        var adminClient = CreateAuthenticatedHttpClient(DatabaseSeeder.Tenant1.Id, _adminUserId, UserRole.Member);

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/account-management/teams/{_teamId}/members/{otherAdminUserId}/role", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();
        Connection.ExecuteScalar<string>("SELECT Role FROM TeamMembers WHERE TeamId = @teamId AND UserId = @userId",
            new { teamId = _teamId.ToString(), userId = otherAdminUserId.ToString() }
        ).Should().Be(nameof(TeamMemberRole.Member));
    }

    [Fact]
    public async Task ChangeTeamMemberRole_WhenTeamAdminTriesToDemoteThemselves_ShouldReturnForbidden()
    {
        // Arrange
        var command = new ChangeTeamMemberRoleCommand(TeamMemberRole.Member);
        var adminClient = CreateAuthenticatedHttpClient(DatabaseSeeder.Tenant1.Id, _adminUserId, UserRole.Member);

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/account-management/teams/{_teamId}/members/{_adminUserId}/role", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Team admins cannot demote themselves from admin role.");
    }

    [Fact]
    public async Task ChangeTeamMemberRole_WhenTenantOwnerChangesAnyMemberRole_ShouldSucceed()
    {
        // Arrange
        var command = new ChangeTeamMemberRoleCommand(TeamMemberRole.Admin);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account-management/teams/{_teamId}/members/{_memberUserId}/role", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();
        Connection.ExecuteScalar<string>("SELECT Role FROM TeamMembers WHERE TeamId = @teamId AND UserId = @userId",
            new { teamId = _teamId.ToString(), userId = _memberUserId.ToString() }
        ).Should().Be(nameof(TeamMemberRole.Admin));
    }

    [Fact]
    public async Task ChangeTeamMemberRole_WhenNonMemberTriesToChangeRole_ShouldReturnForbidden()
    {
        // Arrange
        var command = new ChangeTeamMemberRoleCommand(TeamMemberRole.Admin);
        var nonMemberClient = CreateAuthenticatedHttpClient(DatabaseSeeder.Tenant1.Id, _nonMemberUserId, UserRole.Member);

        // Act
        var response = await nonMemberClient.PutAsJsonAsync($"/api/account-management/teams/{_teamId}/members/{_memberUserId}/role", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only team admins or tenant owners can change member roles.");
    }

    [Fact]
    public async Task ChangeTeamMemberRole_WhenRegularMemberTriesToChangeRole_ShouldReturnForbidden()
    {
        // Arrange
        var command = new ChangeTeamMemberRoleCommand(TeamMemberRole.Admin);
        var memberClient = CreateAuthenticatedHttpClient(DatabaseSeeder.Tenant1.Id, _memberUserId, UserRole.Member);

        // Act - member tries to promote themselves to admin
        var response = await memberClient.PutAsJsonAsync($"/api/account-management/teams/{_teamId}/members/{_memberUserId}/role", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only team admins or tenant owners can change member roles.");
    }

    [Fact]
    public async Task ChangeTeamMemberRole_WhenChangingRoleOfNonExistentMember_ShouldReturnNotFound()
    {
        // Arrange
        var fakeUserId = UserId.NewId();
        var command = new ChangeTeamMemberRoleCommand(TeamMemberRole.Admin);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account-management/teams/{_teamId}/members/{fakeUserId}/role", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"Team member with user ID '{fakeUserId}' not found in team '{_teamId}'.");
    }

    [Fact]
    public async Task ChangeTeamMemberRole_WhenChangingRoleInNonExistentTeam_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentTeamId = TeamId.NewId();
        var command = new ChangeTeamMemberRoleCommand(TeamMemberRole.Admin);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account-management/teams/{nonExistentTeamId}/members/{_memberUserId}/role", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"Team with ID '{nonExistentTeamId}' not found.");
    }

    [Fact]
    public async Task ChangeTeamMemberRole_WhenSuccessful_ShouldCollectTelemetryWithOldAndNewRoles()
    {
        // Arrange
        var command = new ChangeTeamMemberRoleCommand(TeamMemberRole.Admin);

        // Act
        await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account-management/teams/{_teamId}/members/{_memberUserId}/role", command);

        // Assert
        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        var telemetryEvent = TelemetryEventsCollectorSpy.CollectedEvents[0];
        telemetryEvent.GetType().Name.Should().Be("TeamMemberRoleChanged");
    }

    [Fact]
    public async Task ChangeTeamMemberRole_WhenUserFromDifferentTenant_ShouldReturnNotFound()
    {
        // Arrange - create team in different tenant
        var differentTenantTeamId = TeamId.NewId();
        Connection.Insert("Teams", [
                ("TenantId", DatabaseSeeder.Tenant2.Id.ToString()),
                ("Id", differentTenantTeamId.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow()),
                ("ModifiedAt", null),
                ("Name", "Other Tenant Team"),
                ("Description", "Team in different tenant")
            ]
        );

        var command = new ChangeTeamMemberRoleCommand(TeamMemberRole.Admin);

        // Act - Tenant1 owner tries to change role in Tenant2's team
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account-management/teams/{differentTenantTeamId}/members/{_memberUserId}/role", command);

        // Assert - Team should not be found due to tenant scoping
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"Team with ID '{differentTenantTeamId}' not found.");
    }

    private HttpClient CreateAuthenticatedHttpClient(TenantId tenantId, UserId userId, UserRole role)
    {
        var userInfo = new UserInfo
        {
            Id = userId,
            Email = $"{userId}@example.com",
            Role = role.ToString(),
            TenantId = tenantId,
            Locale = "en-US"
        };
        return CreateAuthenticatedClient(userInfo);
    }
}
