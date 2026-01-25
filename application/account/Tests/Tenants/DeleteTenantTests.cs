using System.Net;
using FluentAssertions;
using PlatformPlatform.Account.Database;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using Xunit;

namespace PlatformPlatform.Account.Tests.Tenants;

public sealed class DeleteTenantTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task DeleteTenant_WhenTenantDoesNotExists_ShouldReturnNotFound()
    {
        // Arrange
        var unknownTenantId = TenantId.NewId();

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/internal-api/account/tenants/{unknownTenantId}");

        //Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"Tenant with id '{unknownTenantId}' not found.");

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteTenant_WhenValid_ShouldSoftDeleteTenant()
    {
        // Arrange
        var existingTenantId = DatabaseSeeder.Tenant1.Id;

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/internal-api/account/tenants/{existingTenantId}");

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();
        Connection.RowExists("Tenants", existingTenantId).Should().BeTrue();
        var deletedAt = Connection.ExecuteScalar<string>("SELECT DeletedAt FROM Tenants WHERE Id = @id", [new { id = existingTenantId.ToString() }]);
        deletedAt.Should().NotBeNullOrEmpty();

        var ownerDeletedAt = Connection.ExecuteScalar<string>("SELECT DeletedAt FROM Users WHERE Id = @id", [new { id = DatabaseSeeder.Tenant1Owner.Id.ToString() }]);
        ownerDeletedAt.Should().BeNull();
        var memberDeletedAt = Connection.ExecuteScalar<string>("SELECT DeletedAt FROM Users WHERE Id = @id", [new { id = DatabaseSeeder.Tenant1Member.Id.ToString() }]);
        memberDeletedAt.Should().BeNull();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("TenantDeleted");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }
}
