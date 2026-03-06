using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Account.Database;
using Account.Features.Users.Commands;
using Account.Features.Users.Domain;
using Account.Features.Users.Shared;
using FluentAssertions;
using SharedKernel.Domain;
using SharedKernel.Tests;
using SharedKernel.Tests.Persistence;
using SharedKernel.Validation;
using Xunit;

namespace Account.Tests.Users;

public sealed class BulkDeleteUsersTests : EndpointBaseTest<AccountDbContext>
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
            Connection.Insert("users", [
                    ("tenant_id", DatabaseSeeder.Tenant1.Id.ToString()),
                    ("id", userId.ToString()),
                    ("created_at", TimeProvider.GetUtcNow().AddMinutes(-10)),
                    ("modified_at", null),
                    ("deleted_at", null),
                    ("email", Faker.Internet.UniqueEmail()),
                    ("first_name", Faker.Person.FirstName),
                    ("last_name", Faker.Person.LastName),
                    ("title", "Test User"),
                    ("role", nameof(UserRole.Member)),
                    ("email_confirmed", true),
                    ("avatar", JsonSerializer.Serialize(new Avatar())),
                    ("locale", "en-US"),
                    ("external_identities", "[]")
                ]
            );
        }

        var command = new BulkDeleteUsersCommand(userIds.ToArray());

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account/users/bulk-delete", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userResponses = await response.Content.ReadFromJsonAsync<UserResponse[]>();
        userResponses.Should().NotBeNull();
        userResponses.Length.Should().Be(3);
        foreach (var userId in userIds)
        {
            Connection.RowExists("users", userId.ToString()).Should().BeTrue();
            var deletedAt = Connection.ExecuteScalar<string>("SELECT deleted_at FROM users WHERE id = @id", [new { id = userId.ToString() }]);
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
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account/users/bulk-delete", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"Users with ids '{unknownUserId}' not found.");
    }

    [Fact]
    public async Task BulkDeleteUsers_WhenDeletingOwnUser_ShouldReturnForbidden()
    {
        // Arrange
        var command = new BulkDeleteUsersCommand([DatabaseSeeder.Tenant1Owner.Id]);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account/users/bulk-delete", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "You cannot delete yourself.");
    }

    [Fact]
    public async Task BulkDeleteUsers_WhenNotOwner_ShouldReturnForbidden()
    {
        // Arrange
        var command = new BulkDeleteUsersCommand([DatabaseSeeder.Tenant1Owner.Id]);

        // Act
        var response = await AuthenticatedMemberHttpClient.PostAsJsonAsync("/api/account/users/bulk-delete", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only owners are allowed to delete other users.");
    }

    [Fact]
    public async Task BulkDeleteUsers_WhenEmptyUserIds_ShouldReturnBadRequest()
    {
        // Arrange
        var command = new BulkDeleteUsersCommand([]);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account/users/bulk-delete", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("UserIds", "At least one user must be selected for deletion.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);
    }

    [Fact]
    public async Task BulkDeleteUsers_WhenMixedConfirmedAndUnconfirmed_ShouldSoftDeleteAll()
    {
        // Arrange
        var confirmedUserId = UserId.NewId();
        var unconfirmedUserId = UserId.NewId();

        Connection.Insert("users", [
                ("tenant_id", DatabaseSeeder.Tenant1.Id.ToString()),
                ("id", confirmedUserId.ToString()),
                ("created_at", TimeProvider.GetUtcNow().AddMinutes(-10)),
                ("modified_at", null),
                ("deleted_at", null),
                ("email", Faker.Internet.UniqueEmail()),
                ("first_name", Faker.Person.FirstName),
                ("last_name", Faker.Person.LastName),
                ("title", "Confirmed User"),
                ("role", nameof(UserRole.Member)),
                ("email_confirmed", true),
                ("avatar", JsonSerializer.Serialize(new Avatar())),
                ("locale", "en-US"),
                ("external_identities", "[]")
            ]
        );

        Connection.Insert("users", [
                ("tenant_id", DatabaseSeeder.Tenant1.Id.ToString()),
                ("id", unconfirmedUserId.ToString()),
                ("created_at", TimeProvider.GetUtcNow().AddMinutes(-10)),
                ("modified_at", null),
                ("deleted_at", null),
                ("email", Faker.Internet.UniqueEmail()),
                ("first_name", null),
                ("last_name", null),
                ("title", null),
                ("role", nameof(UserRole.Member)),
                ("email_confirmed", false),
                ("avatar", JsonSerializer.Serialize(new Avatar())),
                ("locale", "en-US"),
                ("external_identities", "[]")
            ]
        );

        var command = new BulkDeleteUsersCommand([confirmedUserId, unconfirmedUserId]);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account/users/bulk-delete", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var userResponses = await response.Content.ReadFromJsonAsync<UserResponse[]>();
        userResponses.Should().NotBeNull();
        userResponses.Length.Should().Be(2);

        Connection.RowExists("users", confirmedUserId.ToString()).Should().BeTrue();
        var confirmedDeletedAt = Connection.ExecuteScalar<string>("SELECT deleted_at FROM users WHERE id = @id", [new { id = confirmedUserId.ToString() }]);
        confirmedDeletedAt.Should().NotBeNullOrEmpty();

        Connection.RowExists("users", unconfirmedUserId.ToString()).Should().BeTrue();
        var unconfirmedDeletedAt = Connection.ExecuteScalar<string>("SELECT deleted_at FROM users WHERE id = @id", [new { id = unconfirmedUserId.ToString() }]);
        unconfirmedDeletedAt.Should().NotBeNullOrEmpty();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(3);
        TelemetryEventsCollectorSpy.CollectedEvents.Count(e => e.GetType().Name == "UserDeleted").Should().Be(2);
        TelemetryEventsCollectorSpy.CollectedEvents.Count(e => e.GetType().Name == "UsersBulkDeleted").Should().Be(1);
    }
}
