using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using PlatformPlatform.Account.Database;
using PlatformPlatform.Account.Features.Users.Commands;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using Xunit;

namespace PlatformPlatform.Account.Tests.Users;

public sealed class BulkPurgeUsersTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task BulkPurgeUsers_WhenOwnerDeletesMultipleDeletedUsers_ShouldPermanentlyDeleteAllSpecifiedUsers()
    {
        // Arrange
        var deletedUserId1 = UserId.NewId();
        var deletedUserId2 = UserId.NewId();
        var deletedUserId3 = UserId.NewId();
        Connection.Insert("Users", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", deletedUserId1.ToString()),
                ("CreatedAt", TimeProvider.GetUtcNow().AddDays(-10)),
                ("ModifiedAt", TimeProvider.GetUtcNow().AddDays(-2)),
                ("DeletedAt", TimeProvider.GetUtcNow().AddDays(-2)),
                ("Email", Faker.Internet.UniqueEmail()),
                ("FirstName", Faker.Person.FirstName),
                ("LastName", Faker.Person.LastName),
                ("Title", "Former Employee 1"),
                ("Role", nameof(UserRole.Member)),
                ("EmailConfirmed", true),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Locale", "en-US"),
                ("ExternalIdentities", "[]")
            ]
        );
        Connection.Insert("Users", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", deletedUserId2.ToString()),
                ("CreatedAt", TimeProvider.GetUtcNow().AddDays(-5)),
                ("ModifiedAt", TimeProvider.GetUtcNow().AddDays(-1)),
                ("DeletedAt", TimeProvider.GetUtcNow().AddDays(-1)),
                ("Email", Faker.Internet.UniqueEmail()),
                ("FirstName", Faker.Person.FirstName),
                ("LastName", Faker.Person.LastName),
                ("Title", "Former Employee 2"),
                ("Role", nameof(UserRole.Member)),
                ("EmailConfirmed", true),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Locale", "en-US"),
                ("ExternalIdentities", "[]")
            ]
        );
        Connection.Insert("Users", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", deletedUserId3.ToString()),
                ("CreatedAt", TimeProvider.GetUtcNow().AddDays(-3)),
                ("ModifiedAt", TimeProvider.GetUtcNow().AddDays(-1)),
                ("DeletedAt", TimeProvider.GetUtcNow().AddDays(-1)),
                ("Email", Faker.Internet.UniqueEmail()),
                ("FirstName", Faker.Person.FirstName),
                ("LastName", Faker.Person.LastName),
                ("Title", "Former Employee 3"),
                ("Role", nameof(UserRole.Member)),
                ("EmailConfirmed", true),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Locale", "en-US"),
                ("ExternalIdentities", "[]")
            ]
        );

        // Act - delete only the first two users, leave the third one
        var command = new BulkPurgeUsersCommand([deletedUserId1, deletedUserId2]);
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account/users/deleted/bulk-purge", command);

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();
        Connection.RowExists("Users", deletedUserId1.ToString()).Should().BeFalse();
        Connection.RowExists("Users", deletedUserId2.ToString()).Should().BeFalse();
        Connection.RowExists("Users", deletedUserId3.ToString()).Should().BeTrue();

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
        Connection.Insert("Users", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", deletedUserId.ToString()),
                ("CreatedAt", TimeProvider.GetUtcNow().AddDays(-10)),
                ("ModifiedAt", TimeProvider.GetUtcNow().AddDays(-2)),
                ("DeletedAt", TimeProvider.GetUtcNow().AddDays(-2)),
                ("Email", Faker.Internet.UniqueEmail()),
                ("FirstName", Faker.Person.FirstName),
                ("LastName", Faker.Person.LastName),
                ("Title", "Former Employee"),
                ("Role", nameof(UserRole.Member)),
                ("EmailConfirmed", true),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Locale", "en-US"),
                ("ExternalIdentities", "[]")
            ]
        );

        // Act
        var command = new BulkPurgeUsersCommand([deletedUserId]);
        var response = await AuthenticatedMemberHttpClient.PostAsJsonAsync("/api/account/users/deleted/bulk-purge", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only owners can permanently delete users from the recycle bin.");
        Connection.RowExists("Users", deletedUserId.ToString()).Should().BeTrue();
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
