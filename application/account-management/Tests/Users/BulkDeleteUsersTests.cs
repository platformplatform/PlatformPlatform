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
    public async Task BulkDeleteUsers_WhenUsersExist_ShouldSoftDeleteUsers()
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
                    ("CreatedAt", TimeProvider.GetUtcNow().AddMinutes(-10)),
                    ("ModifiedAt", null),
                    ("DeletedAt", null),
                    ("Email", Faker.Internet.UniqueEmail()),
                    ("FirstName", Faker.Person.FirstName),
                    ("LastName", Faker.Person.LastName),
                    ("Title", "Test User"),
                    ("Role", nameof(UserRole.Member)),
                    ("EmailConfirmed", true),
                    ("Avatar", JsonSerializer.Serialize(new Avatar())),
                    ("Locale", "en-US"),
                    ("ExternalIdentities", "[]")
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
            Connection.RowExists("Users", userId.ToString()).Should().BeTrue();
            var deletedAt = Connection.ExecuteScalar<string>("SELECT DeletedAt FROM Users WHERE Id = @id", [new { id = userId.ToString() }]);
            deletedAt.Should().NotBeNullOrEmpty();
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

    [Fact]
    public async Task BulkDeleteUsers_WhenMixedConfirmedAndUnconfirmed_ShouldSoftDeleteConfirmedAndHardDeleteUnconfirmed()
    {
        // Arrange
        var confirmedUserId = UserId.NewId();
        var unconfirmedUserId = UserId.NewId();

        Connection.Insert("Users", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", confirmedUserId.ToString()),
                ("CreatedAt", TimeProvider.GetUtcNow().AddMinutes(-10)),
                ("ModifiedAt", null),
                ("DeletedAt", null),
                ("Email", Faker.Internet.UniqueEmail()),
                ("FirstName", Faker.Person.FirstName),
                ("LastName", Faker.Person.LastName),
                ("Title", "Confirmed User"),
                ("Role", nameof(UserRole.Member)),
                ("EmailConfirmed", true),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Locale", "en-US"),
                ("ExternalIdentities", "[]")
            ]
        );

        Connection.Insert("Users", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", unconfirmedUserId.ToString()),
                ("CreatedAt", TimeProvider.GetUtcNow().AddMinutes(-10)),
                ("ModifiedAt", null),
                ("DeletedAt", null),
                ("Email", Faker.Internet.UniqueEmail()),
                ("FirstName", null),
                ("LastName", null),
                ("Title", null),
                ("Role", nameof(UserRole.Member)),
                ("EmailConfirmed", false),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Locale", "en-US"),
                ("ExternalIdentities", "[]")
            ]
        );

        var command = new BulkDeleteUsersCommand([confirmedUserId, unconfirmedUserId]);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account-management/users/bulk-delete", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();

        Connection.RowExists("Users", confirmedUserId.ToString()).Should().BeTrue();
        var deletedAt = Connection.ExecuteScalar<string>("SELECT DeletedAt FROM Users WHERE Id = @id", [new { id = confirmedUserId.ToString() }]);
        deletedAt.Should().NotBeNullOrEmpty();

        Connection.RowExists("Users", unconfirmedUserId.ToString()).Should().BeFalse();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(3);
        TelemetryEventsCollectorSpy.CollectedEvents.Count(e => e.GetType().Name == "UserDeleted").Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents.Count(e => e.GetType().Name == "UserPurged").Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents.Count(e => e.GetType().Name == "UsersBulkDeleted").Should().Be(1);
    }
}
