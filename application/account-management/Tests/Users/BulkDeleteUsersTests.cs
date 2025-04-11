using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.Users.Commands;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using PlatformPlatform.SharedKernel.Validation;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Users;

public sealed class BulkDeleteUsersTests : EndpointBaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task BulkDeleteUsers_WhenUsersExist_ShouldDeleteUsers()
    {
        // Arrange
        var userIds = new List<UserId>();
        for (var i = 0; i < 3; i++)
        {
            var userId = UserId.NewId();
            userIds.Add(userId);
            Connection.Insert("Users", [
                    ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                    ("Id", userId.ToString()),
                    ("CreatedAt", TimeProvider.System.GetUtcNow().AddMinutes(-10)),
                    ("ModifiedAt", null),
                    ("Email", Faker.Internet.Email()),
                    ("FirstName", Faker.Person.FirstName),
                    ("LastName", Faker.Person.LastName),
                    ("Title", "Test User"),
                    ("Role", UserRole.Member.ToString()),
                    ("EmailConfirmed", true),
                    ("Avatar", JsonSerializer.Serialize(new Avatar())),
                    ("Locale", "en-US")
                ]
            );
        }

        var command = new BulkDeleteUsersCommand(userIds.ToArray());

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account-management/users/bulk-delete", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();
        foreach (var userId in userIds)
        {
            Connection.RowExists("Users", userId.ToString()).Should().BeFalse();
        }

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(4);
        foreach (var @event in TelemetryEventsCollectorSpy.CollectedEvents.Take(3))
        {
            @event.GetType().Name.Should().Be("UserDeleted");
            @event.Properties.Should().ContainKey("event.bulk_deletion");
            @event.Properties["event.bulk_deletion"].Should().Be("True");
        }

        TelemetryEventsCollectorSpy.CollectedEvents[3].GetType().Name.Should().Be("UsersBulkDeleted");
        TelemetryEventsCollectorSpy.CollectedEvents[3].Properties["event.count"].Should().Be("3");

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task BulkDeleteUsers_WhenUserDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var unknownUserId = UserId.NewId();
        var command = new BulkDeleteUsersCommand([unknownUserId]);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account-management/users/bulk-delete", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"Users with ids '{unknownUserId}' not found.");
    }

    [Fact]
    public async Task BulkDeleteUsers_WhenDeletingOwnUser_ShouldReturnForbidden()
    {
        // Arrange
        var command = new BulkDeleteUsersCommand([DatabaseSeeder.Tenant1Owner.Id]);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account-management/users/bulk-delete", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "You cannot delete yourself.");
    }

    [Fact]
    public async Task BulkDeleteUsers_WhenNotOwner_ShouldReturnForbidden()
    {
        // Arrange
        var command = new BulkDeleteUsersCommand([DatabaseSeeder.Tenant1Owner.Id]);

        // Act
        var response = await AuthenticatedMemberHttpClient.PostAsJsonAsync("/api/account-management/users/bulk-delete", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only owners are allowed to delete other users.");
    }

    [Fact]
    public async Task BulkDeleteUsers_WhenEmptyUserIds_ShouldReturnBadRequest()
    {
        // Arrange
        var command = new BulkDeleteUsersCommand([]);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account-management/users/bulk-delete", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("UserIds", "At least one user must be selected for deletion.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);
    }
}
