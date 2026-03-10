using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Account.Database;
using Account.Features.Users.Commands;
using Account.Features.Users.Domain;
using FluentAssertions;
using SharedKernel.Domain;
using SharedKernel.Tests;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Users;

public sealed class BulkPurgeUsersTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task BulkPurgeUsers_WhenOwnerDeletesMultipleDeletedUsers_ShouldPermanentlyDeleteAllSpecifiedUsers()
    {
        // Arrange
        var deletedUserId1 = UserId.NewId();
        var deletedUserId2 = UserId.NewId();
        var deletedUserId3 = UserId.NewId();
        Connection.Insert("users", [
                ("tenant_id", DatabaseSeeder.Tenant1.Id.ToString()),
                ("id", deletedUserId1.ToString()),
                ("created_at", TimeProvider.GetUtcNow().AddDays(-10)),
                ("modified_at", TimeProvider.GetUtcNow().AddDays(-2)),
                ("deleted_at", TimeProvider.GetUtcNow().AddDays(-2)),
                ("email", Faker.Internet.UniqueEmail()),
                ("first_name", Faker.Person.FirstName),
                ("last_name", Faker.Person.LastName),
                ("title", "Former Employee 1"),
                ("role", nameof(UserRole.Member)),
                ("email_confirmed", true),
                ("avatar", JsonSerializer.Serialize(new Avatar())),
                ("locale", "en-US"),
                ("external_identities", "[]"),
                ("rollout_bucket", 42)
            ]
        );
        Connection.Insert("users", [
                ("tenant_id", DatabaseSeeder.Tenant1.Id.ToString()),
                ("id", deletedUserId2.ToString()),
                ("created_at", TimeProvider.GetUtcNow().AddDays(-5)),
                ("modified_at", TimeProvider.GetUtcNow().AddDays(-1)),
                ("deleted_at", TimeProvider.GetUtcNow().AddDays(-1)),
                ("email", Faker.Internet.UniqueEmail()),
                ("first_name", Faker.Person.FirstName),
                ("last_name", Faker.Person.LastName),
                ("title", "Former Employee 2"),
                ("role", nameof(UserRole.Member)),
                ("email_confirmed", true),
                ("avatar", JsonSerializer.Serialize(new Avatar())),
                ("locale", "en-US"),
                ("external_identities", "[]"),
                ("rollout_bucket", 42)
            ]
        );
        Connection.Insert("users", [
                ("tenant_id", DatabaseSeeder.Tenant1.Id.ToString()),
                ("id", deletedUserId3.ToString()),
                ("created_at", TimeProvider.GetUtcNow().AddDays(-3)),
                ("modified_at", TimeProvider.GetUtcNow().AddDays(-1)),
                ("deleted_at", TimeProvider.GetUtcNow().AddDays(-1)),
                ("email", Faker.Internet.UniqueEmail()),
                ("first_name", Faker.Person.FirstName),
                ("last_name", Faker.Person.LastName),
                ("title", "Former Employee 3"),
                ("role", nameof(UserRole.Member)),
                ("email_confirmed", true),
                ("avatar", JsonSerializer.Serialize(new Avatar())),
                ("locale", "en-US"),
                ("external_identities", "[]"),
                ("rollout_bucket", 42)
            ]
        );

        // Act - delete only the first two users, leave the third one
        var command = new BulkPurgeUsersCommand([deletedUserId1, deletedUserId2]);
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account/users/deleted/bulk-purge", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();
        Connection.RowExists("users", deletedUserId1.ToString()).Should().BeFalse();
        Connection.RowExists("users", deletedUserId2.ToString()).Should().BeFalse();
        Connection.RowExists("users", deletedUserId3.ToString()).Should().BeTrue();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(2);
        TelemetryEventsCollectorSpy.CollectedEvents.Should().AllSatisfy(e =>
            {
                e.GetType().Name.Should().Be("UserPurged");
                e.Properties["event.reason"].Should().Be(nameof(UserPurgeReason.BulkUserPurge));
            }
        );
    }

    [Fact]
    public async Task BulkPurgeUsers_WhenMember_ShouldReturnForbidden()
    {
        // Arrange
        var deletedUserId = UserId.NewId();
        Connection.Insert("users", [
                ("tenant_id", DatabaseSeeder.Tenant1.Id.ToString()),
                ("id", deletedUserId.ToString()),
                ("created_at", TimeProvider.GetUtcNow().AddDays(-10)),
                ("modified_at", TimeProvider.GetUtcNow().AddDays(-2)),
                ("deleted_at", TimeProvider.GetUtcNow().AddDays(-2)),
                ("email", Faker.Internet.UniqueEmail()),
                ("first_name", Faker.Person.FirstName),
                ("last_name", Faker.Person.LastName),
                ("title", "Former Employee"),
                ("role", nameof(UserRole.Member)),
                ("email_confirmed", true),
                ("avatar", JsonSerializer.Serialize(new Avatar())),
                ("locale", "en-US"),
                ("external_identities", "[]"),
                ("rollout_bucket", 42)
            ]
        );

        // Act
        var command = new BulkPurgeUsersCommand([deletedUserId]);
        var response = await AuthenticatedMemberHttpClient.PostAsJsonAsync("/api/account/users/deleted/bulk-purge", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only owners can permanently delete users from the recycle bin.");
        Connection.RowExists("users", deletedUserId.ToString()).Should().BeTrue();
    }

    [Fact]
    public async Task BulkPurgeUsers_WhenUserNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentUserId = UserId.NewId();

        // Act
        var command = new BulkPurgeUsersCommand([nonExistentUserId]);
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account/users/deleted/bulk-purge", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"Deleted users with ids '{nonExistentUserId}' not found.");
    }

    [Fact]
    public async Task BulkPurgeUsers_WhenEmptyUserIds_ShouldReturnBadRequest()
    {
        // Act
        var command = new BulkPurgeUsersCommand([]);
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account/users/deleted/bulk-purge", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
