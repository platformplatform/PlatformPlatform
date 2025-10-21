using System.Net;
using System.Text.Json;
using FluentAssertions;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.TeamMembers.Domain;
using PlatformPlatform.AccountManagement.Features.TeamMembers.Queries;
using PlatformPlatform.AccountManagement.Features.Teams.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.TeamMembers;

public sealed class GetTeamMembersTests : EndpointBaseTest<AccountManagementDbContext>
{
    private readonly UserId _adminUserId = UserId.NewId();
    private readonly UserId _memberUserId = UserId.NewId();
    private readonly UserId _nonMemberUserId = UserId.NewId();
    private readonly TeamId _teamId = TeamId.NewId();

    public GetTeamMembersTests()
    {
        Connection.Insert("Teams", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", _teamId.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow().AddMinutes(-30)),
                ("ModifiedAt", null),
                ("Name", "Test Team"),
                ("Description", "Test description")
            ]
        );

        Connection.Insert("Users", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", _adminUserId.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow().AddMinutes(-20)),
                ("ModifiedAt", null),
                ("Email", "admin@test.com"),
                ("FirstName", "Alice"),
                ("LastName", "Admin"),
                ("Title", "Team Lead"),
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
                ("FirstName", "Bob"),
                ("LastName", "Member"),
                ("Title", "Developer"),
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
                ("FirstName", "Charlie"),
                ("LastName", "NonMember"),
                ("Title", "Designer"),
                ("Role", nameof(UserRole.Member)),
                ("EmailConfirmed", true),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Locale", "en-US")
            ]
        );

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
    public async Task GetTeamMembers_WhenTeamMemberViewsTeam_ShouldSucceed()
    {
        var memberClient = CreateAuthenticatedHttpClient(DatabaseSeeder.Tenant1.Id, _memberUserId, UserRole.Member);

        var response = await memberClient.GetAsync($"/api/account-management/teams/{_teamId}/members");

        response.ShouldBeSuccessfulGetRequest();
        var content = await response.DeserializeResponse<TeamMembersResponse>();
        content.Should().NotBeNull();
        content.Members.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTeamMembers_WhenTenantOwnerViewsTeam_ShouldSucceed()
    {
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/api/account-management/teams/{_teamId}/members");

        response.ShouldBeSuccessfulGetRequest();
        var content = await response.DeserializeResponse<TeamMembersResponse>();
        content.Should().NotBeNull();
        content.Members.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTeamMembers_WhenNonMemberTriesToView_ShouldReturnForbidden()
    {
        var nonMemberClient = CreateAuthenticatedHttpClient(DatabaseSeeder.Tenant1.Id, _nonMemberUserId, UserRole.Member);

        var response = await nonMemberClient.GetAsync($"/api/account-management/teams/{_teamId}/members");

        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only team members or tenant owners can view team members.");
    }

    [Fact]
    public async Task GetTeamMembers_WhenTeamHasNoMembers_ShouldReturnEmptyArray()
    {
        var emptyTeamId = TeamId.NewId();
        Connection.Insert("Teams", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", emptyTeamId.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow()),
                ("ModifiedAt", null),
                ("Name", "Empty Team"),
                ("Description", "No members")
            ]
        );

        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/api/account-management/teams/{emptyTeamId}/members");

        response.ShouldBeSuccessfulGetRequest();
        var content = await response.DeserializeResponse<TeamMembersResponse>();
        content.Should().NotBeNull();
        content.Members.Should().BeEmpty();
    }

    [Fact]
    public async Task GetTeamMembers_WhenSortingByRole_AdminsShouldBeFirst()
    {
        var memberClient = CreateAuthenticatedHttpClient(DatabaseSeeder.Tenant1.Id, _memberUserId, UserRole.Member);

        var response = await memberClient.GetAsync($"/api/account-management/teams/{_teamId}/members");

        response.ShouldBeSuccessfulGetRequest();
        var content = await response.DeserializeResponse<TeamMembersResponse>();
        content.Should().NotBeNull();
        content.Members.Should().HaveCount(2);
        content.Members[0].Role.Should().Be(TeamMemberRole.Admin);
        content.Members[1].Role.Should().Be(TeamMemberRole.Member);
    }

    [Fact]
    public async Task GetTeamMembers_ReturnsCorrectUserDetails()
    {
        var memberClient = CreateAuthenticatedHttpClient(DatabaseSeeder.Tenant1.Id, _memberUserId, UserRole.Member);

        var response = await memberClient.GetAsync($"/api/account-management/teams/{_teamId}/members");

        response.ShouldBeSuccessfulGetRequest();
        var content = await response.DeserializeResponse<TeamMembersResponse>();
        content.Should().NotBeNull();
        var adminMember = content.Members.First(m => m.Role == TeamMemberRole.Admin);
        adminMember.UserName.Should().Be("Alice Admin");
        adminMember.UserEmail.Should().Be("admin@test.com");
        adminMember.UserTitle.Should().Be("Team Lead");
        adminMember.UserId.Should().Be(_adminUserId);
    }

    [Fact]
    public async Task GetTeamMembers_WhenTeamDoesNotExist_ShouldReturnNotFound()
    {
        var nonExistentTeamId = TeamId.NewId();

        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/api/account-management/teams/{nonExistentTeamId}/members");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTeamMembers_WhenAccessingDifferentTenantTeam_ShouldReturnNotFound()
    {
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

        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/api/account-management/teams/{differentTenantTeamId}/members");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
