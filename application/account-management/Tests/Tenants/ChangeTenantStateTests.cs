using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.Tenants.Commands;
using PlatformPlatform.AccountManagement.Features.Tenants.Domain;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Tenants;

public sealed class ChangeTenantStateTests : EndpointBaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task ChangeTenantState_WhenValidStateTransition_ShouldUpdateStateAndHistory()
    {
        // Arrange
        var command = new ChangeTenantStateCommand(TenantState.Active);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync("/api/account-management/tenants/current/state", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        var state = Connection.ExecuteScalar<string>(
            "SELECT State FROM Tenants WHERE Id = @id", new { id = DatabaseSeeder.Tenant1.Id.ToString() }
        );
        state.Should().Be(TenantState.Active.ToString());

        var stateHistory = Connection.ExecuteScalar<string>(
            "SELECT StateHistory FROM Tenants WHERE Id = @id", new { id = DatabaseSeeder.Tenant1.Id.ToString() }
        );
        stateHistory.Should().NotBeNull();
        stateHistory.Should().Contain(TenantState.Active.ToString());

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("TenantStateChanged");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task ChangeTenantState_WhenSameState_ShouldReturnBadRequest()
    {
        // Arrange
        var command = new ChangeTenantStateCommand(TenantState.Trial);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync("/api/account-management/tenants/current/state", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, $"Tenant is already in state '{TenantState.Trial}'.");

        var state = Connection.ExecuteScalar<string>(
            "SELECT State FROM Tenants WHERE Id = @id", new { id = DatabaseSeeder.Tenant1.Id.ToString() }
        );
        state.Should().Be(TenantState.Trial.ToString());
    }

    [Fact]
    public async Task ChangeTenantState_WhenMemberUser_ShouldReturnForbidden()
    {
        // Arrange
        var command = new ChangeTenantStateCommand(TenantState.Active);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AuthenticatedMemberHttpClient
            .PutAsJsonAsync("/api/account-management/tenants/current/state", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only owners are allowed to change tenant state.");

        var state = Connection.ExecuteScalar<string>(
            "SELECT State FROM Tenants WHERE Id = @id", new { id = DatabaseSeeder.Tenant1.Id.ToString() }
        );
        state.Should().Be(TenantState.Trial.ToString());
    }

    [Fact]
    public async Task ChangeTenantState_WhenNotOwner_ShouldReturnForbidden()
    {
        // Arrange
        var command = new ChangeTenantStateCommand(TenantState.Active);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AuthenticatedMemberHttpClient.PutAsJsonAsync("/api/account-management/tenants/current/state", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only owners are allowed to change tenant state.");

        var state = Connection.ExecuteScalar<string>(
            "SELECT State FROM Tenants WHERE Id = @id", new { id = DatabaseSeeder.Tenant1.Id.ToString() }
        );
        state.Should().Be(TenantState.Trial.ToString());
    }
}
