using System.Net;
using System.Net.Http.Json;
using Account.Database;
using Account.Features.Users.Commands;
using Account.Features.Users.Domain;
using Account.Features.Users.Shared;
using FluentAssertions;
using SharedKernel.Domain;
using SharedKernel.Tests;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Users;

public sealed class ChangeUserRoleTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task ChangeUserRole_WhenOwnerChangesAnotherUserRole_ShouldSucceed()
    {
        // Arrange
        var command = new ChangeUserRoleCommand { UserRole = UserRole.Owner };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync(
            $"/api/account/users/{DatabaseSeeder.Tenant1Member.Id}/change-user-role", command
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userResponse = await response.Content.ReadFromJsonAsync<UserResponse>();
        userResponse.Should().NotBeNull();
        userResponse.Id.Should().Be(DatabaseSeeder.Tenant1Member.Id);
        userResponse.Role.Should().Be(UserRole.Owner);

        var updatedRole = Connection.ExecuteScalar<string>(
            "SELECT role FROM users WHERE id = @id", [new { id = DatabaseSeeder.Tenant1Member.Id.ToString() }]
        );
        updatedRole.Should().Be(nameof(UserRole.Owner));

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("UserRoleChanged");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.user_id"].Should().Be(DatabaseSeeder.Tenant1Member.Id.ToString());
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.from_role"].Should().Be(nameof(UserRole.Member));
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.to_role"].Should().Be(nameof(UserRole.Owner));
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task ChangeUserRole_WhenOwnerChangesRoleFromOwnerToMember_ShouldSucceed()
    {
        // Arrange
        Connection.Update("users", "id", DatabaseSeeder.Tenant1Member.Id.ToString(), [("role", nameof(UserRole.Owner))]);
        var command = new ChangeUserRoleCommand { UserRole = UserRole.Member };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync(
            $"/api/account/users/{DatabaseSeeder.Tenant1Member.Id}/change-user-role", command
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userResponse = await response.Content.ReadFromJsonAsync<UserResponse>();
        userResponse.Should().NotBeNull();
        userResponse.Id.Should().Be(DatabaseSeeder.Tenant1Member.Id);
        userResponse.Role.Should().Be(UserRole.Member);

        var updatedRole = Connection.ExecuteScalar<string>(
            "SELECT role FROM users WHERE id = @id", [new { id = DatabaseSeeder.Tenant1Member.Id.ToString() }]
        );
        updatedRole.Should().Be(nameof(UserRole.Member));

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("UserRoleChanged");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.from_role"].Should().Be(nameof(UserRole.Owner));
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.to_role"].Should().Be(nameof(UserRole.Member));
    }

    [Fact]
    public async Task ChangeUserRole_WhenOwnerTriesToChangeTheirOwnRole_ShouldReturnForbidden()
    {
        // Arrange
        var command = new ChangeUserRoleCommand { UserRole = UserRole.Member };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync(
            $"/api/account/users/{DatabaseSeeder.Tenant1Owner.Id}/change-user-role", command
        );

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "You cannot change your own user role.");

        var roleUnchanged = Connection.ExecuteScalar<string>(
            "SELECT role FROM users WHERE id = @id", [new { id = DatabaseSeeder.Tenant1Owner.Id.ToString() }]
        );
        roleUnchanged.Should().Be(nameof(UserRole.Owner));

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task ChangeUserRole_WhenMemberTriesToChangeRole_ShouldReturnForbidden()
    {
        // Arrange
        var command = new ChangeUserRoleCommand { UserRole = UserRole.Owner };

        // Act
        var response = await AuthenticatedMemberHttpClient.PutAsJsonAsync(
            $"/api/account/users/{DatabaseSeeder.Tenant1Owner.Id}/change-user-role", command
        );

        // Assert
        await response.ShouldHaveErrorStatusCode(
            HttpStatusCode.Forbidden, "Only owners are allowed to change the user roles of users."
        );

        var roleUnchanged = Connection.ExecuteScalar<string>(
            "SELECT role FROM users WHERE id = @id", [new { id = DatabaseSeeder.Tenant1Owner.Id.ToString() }]
        );
        roleUnchanged.Should().Be(nameof(UserRole.Owner));

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task ChangeUserRole_WhenUserNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentUserId = UserId.NewId();
        var command = new ChangeUserRoleCommand { UserRole = UserRole.Owner };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PutAsJsonAsync(
            $"/api/account/users/{nonExistentUserId}/change-user-role", command
        );

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"User with id '{nonExistentUserId}' not found.");

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task ChangeUserRole_WhenNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var command = new ChangeUserRoleCommand { UserRole = UserRole.Owner };

        // Act
        var response = await AnonymousHttpClient.PutAsJsonAsync(
            $"/api/account/users/{DatabaseSeeder.Tenant1Member.Id}/change-user-role", command
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }
}
