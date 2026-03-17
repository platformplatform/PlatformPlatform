using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Account.Database;
using Account.Features.Users.Domain;
using FluentAssertions;
using SharedKernel.Domain;
using SharedKernel.Tests;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Users;

public sealed class EmptyRecycleBinTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task EmptyRecycleBin_WhenOwnerEmptiesRecycleBin_ShouldPermanentlyDeleteAllUsers()
    {
        // Arrange
        var deletedUserId1 = UserId.NewId();
        var deletedUserId2 = UserId.NewId();
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
                ("external_identities", "[]")
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
                ("external_identities", "[]")
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsync("/api/account/users/deleted/empty-recycle-bin", null);

        // Assert
        response.EnsureSuccessStatusCode();
        var deletedCount = await response.Content.ReadFromJsonAsync<int>();
        deletedCount.Should().Be(2);
        Connection.RowExists("users", deletedUserId1.ToString()).Should().BeFalse();
        Connection.RowExists("users", deletedUserId2.ToString()).Should().BeFalse();

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
        var response = await AuthenticatedMemberHttpClient.PostAsync("/api/account/users/deleted/empty-recycle-bin", null);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only owners can empty the deleted users recycle bin.");
    }

    [Fact]
    public async Task EmptyRecycleBin_WhenNoDeletedUsers_ShouldReturnZero()
    {
        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsync("/api/account/users/deleted/empty-recycle-bin", null);

        // Assert
        response.EnsureSuccessStatusCode();
        var deletedCount = await response.Content.ReadFromJsonAsync<int>();
        deletedCount.Should().Be(0);

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }
}
