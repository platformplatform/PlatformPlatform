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

public sealed class UpdateTeamMembersTests : EndpointBaseTest<AccountManagementDbContext>
{
    private readonly UserId _adminUserId = UserId.NewId();
    private readonly UserId _memberUserId = UserId.NewId();
    private readonly UserId _nonMemberUserId = UserId.NewId();
    private readonly TeamId _teamId = TeamId.NewId();

    public UpdateTeamMembersTests()
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
                ("Role", (int)TeamMemberRole.Admin)
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
                ("Role", (int)TeamMemberRole.Member)
            ]
        );
    }

    [Fact]
    public async Task UpdateTeamMembers_WhenTeamAdminAddsAndRemovesMembers_ShouldSucceed()
    {
        // Arrange
        var newUserId = UserId.NewId();
        Connection.Insert("Users", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", newUserId.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow().AddMinutes(-5)),
                ("ModifiedAt", null),
                ("Email", "newuser@test.com"),
                ("FirstName", "New"),
                ("LastName", "User"),
                ("Title", "New User"),
                ("Role", nameof(UserRole.Member)),
                ("EmailConfirmed", true),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Locale", "en-US")
            ]
        );

        var command = new UpdateTeamMembersCommand(
            [new MemberToAdd(newUserId, TeamMemberRole.Member)],
            [_memberUserId]
        );

        var adminClient = CreateAuthenticatedHttpClient(DatabaseSeeder.Tenant1.Id, _adminUserId, UserRole.Member);

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/account-management/teams/{_teamId}/members", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();
        Connection.ExecuteScalar<int>("SELECT COUNT(*) FROM TeamMembers WHERE TeamId = @teamId AND UserId = @userId",
            new { teamId = _teamId.ToString(), userId = newUserId.ToString() }
        ).Should().Be(1);
        Connection.ExecuteScalar<int>("SELECT COUNT(*) FROM TeamMembers WHERE TeamId = @teamId AND UserId = @userId",
            new { teamId = _teamId.ToString(), userId = _memberUserId.ToString() }
        ).Should().Be(0);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(3); // TeamMemberAdded + TeamMemberRemoved + TeamMembersUpdated
        TelemetryEventsCollectorSpy.CollectedEvents.Should().ContainSingle(e => e.GetType().Name == "TeamMemberAdded");
        TelemetryEventsCollectorSpy.CollectedEvents.Should().ContainSingle(e => e.GetType().Name == "TeamMemberRemoved");
        TelemetryEventsCollectorSpy.CollectedEvents.Should().ContainSingle(e => e.GetType().Name == "TeamMembersUpdated");
    }

    [Fact]
    public async Task UpdateTeamMembers_WhenTenantOwnerAddsMembers_ShouldSucceed()
    {
        // Arrange
        var command = new UpdateTeamMembersCommand(
            [new MemberToAdd(_nonMemberUserId, TeamMemberRole.Member)],
            []
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account-management/teams/{_teamId}/members", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();
        Connection.ExecuteScalar<int>("SELECT COUNT(*) FROM TeamMembers WHERE TeamId = @teamId AND UserId = @userId",
            new { teamId = _teamId.ToString(), userId = _nonMemberUserId.ToString() }
        ).Should().Be(1);
    }

    [Fact]
    public async Task UpdateTeamMembers_WhenAddingMixedRoles_ShouldSucceed()
    {
        // Arrange
        var newAdmin = UserId.NewId();
        var newMember = UserId.NewId();

        Connection.Insert("Users", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", newAdmin.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow()),
                ("ModifiedAt", null),
                ("Email", "newadmin@test.com"),
                ("FirstName", "New"),
                ("LastName", "Admin"),
                ("Title", "Admin"),
                ("Role", nameof(UserRole.Member)),
                ("EmailConfirmed", true),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Locale", "en-US")
            ]
        );

        Connection.Insert("Users", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", newMember.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow()),
                ("ModifiedAt", null),
                ("Email", "newmember2@test.com"),
                ("FirstName", "New"),
                ("LastName", "Member"),
                ("Title", "Member"),
                ("Role", nameof(UserRole.Member)),
                ("EmailConfirmed", true),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Locale", "en-US")
            ]
        );

        var command = new UpdateTeamMembersCommand(
            [
                new MemberToAdd(newAdmin, TeamMemberRole.Admin),
                new MemberToAdd(newMember, TeamMemberRole.Member)
            ],
            []
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account-management/teams/{_teamId}/members", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();
        Connection.ExecuteScalar<int>("SELECT Role FROM TeamMembers WHERE TeamId = @teamId AND UserId = @userId",
            new { teamId = _teamId.ToString(), userId = newAdmin.ToString() }
        ).Should().Be((int)TeamMemberRole.Admin);
        Connection.ExecuteScalar<int>("SELECT Role FROM TeamMembers WHERE TeamId = @teamId AND UserId = @userId",
            new { teamId = _teamId.ToString(), userId = newMember.ToString() }
        ).Should().Be((int)TeamMemberRole.Member);
    }

    [Fact]
    public async Task UpdateTeamMembers_WhenRegularMemberTriesToUpdate_ShouldReturnForbidden()
    {
        // Arrange
        var command = new UpdateTeamMembersCommand(
            [new MemberToAdd(_nonMemberUserId, TeamMemberRole.Member)],
            []
        );

        var memberClient = CreateAuthenticatedHttpClient(DatabaseSeeder.Tenant1.Id, _memberUserId, UserRole.Member);

        // Act
        var response = await memberClient.PutAsJsonAsync($"/api/account-management/teams/{_teamId}/members", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only team admins or tenant owners can update team members.");
    }

    [Fact]
    public async Task UpdateTeamMembers_WhenNonMemberTriesToUpdate_ShouldReturnForbidden()
    {
        // Arrange
        var command = new UpdateTeamMembersCommand(
            [new MemberToAdd(_nonMemberUserId, TeamMemberRole.Member)],
            []
        );

        var nonMemberClient = CreateAuthenticatedHttpClient(DatabaseSeeder.Tenant1.Id, _nonMemberUserId, UserRole.Member);

        // Act
        var response = await nonMemberClient.PutAsJsonAsync($"/api/account-management/teams/{_teamId}/members", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only team admins or tenant owners can update team members.");
    }

    [Fact]
    public async Task UpdateTeamMembers_WhenAddingDuplicateMembers_ShouldSilentlySkipDuplicates()
    {
        // Arrange - try to add existing member again
        var command = new UpdateTeamMembersCommand(
            [new MemberToAdd(_memberUserId, TeamMemberRole.Member)],
            []
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account-management/teams/{_teamId}/members", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();
        Connection.ExecuteScalar<int>("SELECT COUNT(*) FROM TeamMembers WHERE TeamId = @teamId AND UserId = @userId",
            new { teamId = _teamId.ToString(), userId = _memberUserId.ToString() }
        ).Should().Be(1); // Still only 1 record

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1); // Only TeamMembersUpdated with 0 added
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("TeamMembersUpdated");
    }

    [Fact]
    public async Task UpdateTeamMembers_WhenAddingNonExistentUsers_ShouldReturnNotFound()
    {
        // Arrange
        var fakeUserId = UserId.NewId();
        var command = new UpdateTeamMembersCommand(
            [new MemberToAdd(fakeUserId, TeamMemberRole.Member)],
            []
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account-management/teams/{_teamId}/members", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"Users not found: {fakeUserId}.");
    }

    [Fact]
    public async Task UpdateTeamMembers_WhenRemovingNonExistentMembers_ShouldReturnNotFound()
    {
        // Arrange
        var command = new UpdateTeamMembersCommand(
            [],
            [_nonMemberUserId] // User exists but is not a team member
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account-management/teams/{_teamId}/members", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"Team members not found for users: {_nonMemberUserId}.");
    }

    [Fact]
    public async Task UpdateTeamMembers_WhenAddingUsersFromDifferentTenant_ShouldReturnForbidden()
    {
        // Arrange - create user in different tenant
        var differentTenantUserId = UserId.NewId();
        Connection.Insert("Users", [
                ("TenantId", DatabaseSeeder.Tenant2.Id.ToString()),
                ("Id", differentTenantUserId.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow()),
                ("ModifiedAt", null),
                ("Email", "othertenantuser@test.com"),
                ("FirstName", "Other"),
                ("LastName", "Tenant"),
                ("Title", "User"),
                ("Role", nameof(UserRole.Member)),
                ("EmailConfirmed", true),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Locale", "en-US")
            ]
        );

        var command = new UpdateTeamMembersCommand(
            [new MemberToAdd(differentTenantUserId, TeamMemberRole.Member)],
            []
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account-management/teams/{_teamId}/members", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Cannot add users from different tenant to team.");
    }

    [Fact]
    public async Task UpdateTeamMembers_WhenTeamAdminTriesToRemoveThemselves_ShouldReturnForbidden()
    {
        // Arrange
        var command = new UpdateTeamMembersCommand(
            [],
            [_adminUserId] // Admin trying to remove themselves
        );

        var adminClient = CreateAuthenticatedHttpClient(DatabaseSeeder.Tenant1.Id, _adminUserId, UserRole.Member);

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/account-management/teams/{_teamId}/members", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Team admins cannot remove themselves from the team.");
    }

    [Fact]
    public async Task UpdateTeamMembers_WhenExceedingMaximumOperations_ShouldReturnBadRequest()
    {
        // Arrange - try to add 101 members
        var membersToAdd = Enumerable.Range(0, 101)
            .Select(_ => new MemberToAdd(UserId.NewId(), TeamMemberRole.Member))
            .ToArray();

        var command = new UpdateTeamMembersCommand(
            membersToAdd,
            []
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account-management/teams/{_teamId}/members", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Total operations (adds + removes) must not exceed 100");
    }

    [Fact]
    public async Task UpdateTeamMembers_WhenEmptyArrays_ShouldSucceed()
    {
        // Arrange
        var command = new UpdateTeamMembersCommand(
            [],
            []
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account-management/teams/{_teamId}/members", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();
        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1); // Only TeamMembersUpdated with 0 added and 0 removed
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("TeamMembersUpdated");
    }

    [Fact]
    public async Task UpdateTeamMembers_WhenTeamDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentTeamId = TeamId.NewId();
        var command = new UpdateTeamMembersCommand(
            [new MemberToAdd(_nonMemberUserId, TeamMemberRole.Member)],
            []
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync($"/api/account-management/teams/{nonExistentTeamId}/members", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"Team with ID '{nonExistentTeamId}' not found.");
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
