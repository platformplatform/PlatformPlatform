using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Users;

public sealed class EmptyRecycleBinTests : EndpointBaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task EmptyRecycleBin_WhenOwnerEmptiesRecycleBin_ShouldPermanentlyDeleteAllUsers()
    {
        // Arrange
        var deletedUserId1 = UserId.NewId();
        var deletedUserId2 = UserId.NewId();
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

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsync("/api/account-management/users/deleted/empty-recycle-bin", null);

        // Assert
        response.EnsureSuccessStatusCode();
        var deletedCount = await response.Content.ReadFromJsonAsync<int>();
        deletedCount.Should().Be(2);
        Connection.RowExists("Users", deletedUserId1.ToString()).Should().BeFalse();
        Connection.RowExists("Users", deletedUserId2.ToString()).Should().BeFalse();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(2);
        TelemetryEventsCollectorSpy.CollectedEvents.Should().AllSatisfy(e =>
            {
                e.GetType().Name.Should().Be("UserPurged");
                e.Properties["event.reason"].Should().Be(nameof(UserPurgeReason.RecycleBinPurge));
            }
        );
    }

    [Fact]
    public async Task EmptyRecycleBin_WhenMember_ShouldReturnForbidden()
    {
        // Act
        var response = await AuthenticatedMemberHttpClient.PostAsync("/api/account-management/users/deleted/empty-recycle-bin", null);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only owners can empty the deleted users recycle bin.");
    }

    [Fact]
    public async Task EmptyRecycleBin_WhenNoDeletedUsers_ShouldReturnZero()
    {
        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsync("/api/account-management/users/deleted/empty-recycle-bin", null);

        // Assert
        response.EnsureSuccessStatusCode();
        var deletedCount = await response.Content.ReadFromJsonAsync<int>();
        deletedCount.Should().Be(0);

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }
}
