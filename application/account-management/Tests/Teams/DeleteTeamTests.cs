using System.Net;
using FluentAssertions;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.Teams.Domain;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Teams;

public sealed class DeleteTeamTests : EndpointBaseTest<AccountManagementDbContext>
{
    private readonly TeamId _teamId = TeamId.NewId();

    public DeleteTeamTests()
    {
        Connection.Insert("Teams", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", _teamId.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow().AddMinutes(-10)),
                ("ModifiedAt", null),
                ("Name", "Test Team"),
                ("Description", "Test description")
            ]
        );
    }

    [Fact]
    public async Task DeleteTeam_WhenValid_ShouldDeleteTeam()
    {
        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/api/account-management/teams/{_teamId}");

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();
        Connection.RowExists("Teams", _teamId.ToString()).Should().BeFalse();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("TeamDeleted");
    }

    [Fact]
    public async Task DeleteTeam_WhenTeamDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentTeamId = TeamId.NewId();

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/api/account-management/teams/{nonExistentTeamId}");

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"Team with ID '{nonExistentTeamId}' not found.");
    }

    [Fact]
    public async Task DeleteTeam_WhenNonOwnerUser_ShouldReturnForbidden()
    {
        // Act
        var response = await AuthenticatedMemberHttpClient.DeleteAsync($"/api/account-management/teams/{_teamId}");

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only tenant owners can delete teams.");
    }

    [Fact]
    public async Task DeleteTeam_WhenTeamFromDifferentTenant_ShouldReturnNotFound()
    {
        // Arrange - Create team for different tenant
        var otherTenantTeamId = TeamId.NewId();
        Connection.Insert("Teams", [
                ("TenantId", DatabaseSeeder.Tenant2.Id.ToString()),
                ("Id", otherTenantTeamId.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow().AddMinutes(-10)),
                ("ModifiedAt", null),
                ("Name", "Other Tenant Team"),
                ("Description", "Other tenant description")
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/api/account-management/teams/{otherTenantTeamId}");

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"Team with ID '{otherTenantTeamId}' not found.");

        // Verify team was not deleted (still exists for other tenant)
        Connection.RowExists("Teams", otherTenantTeamId.ToString()).Should().BeTrue();
    }
}
