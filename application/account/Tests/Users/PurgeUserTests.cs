using System.Net;
using System.Text.Json;
using Account.Database;
using Account.Features.Users.Domain;
using FluentAssertions;
using SharedKernel.Domain;
using SharedKernel.Tests;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Users;

public sealed class PurgeUserTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task PurgeUser_WhenOwnerDeletesSoftDeletedUser_ShouldSucceed()
    {
        // Arrange
        var deletedUserId = UserId.NewId();
        Connection.Insert("users", [
                ("tenant_id", DatabaseSeeder.Tenant1.Id.ToString()),
                ("id", deletedUserId.ToString()),
                ("created_at", TimeProvider.GetUtcNow().AddDays(-10)),
                ("modified_at", TimeProvider.GetUtcNow().AddDays(-1)),
                ("deleted_at", TimeProvider.GetUtcNow().AddDays(-1)),
                ("email", Faker.Internet.UniqueEmail()),
                ("first_name", Faker.Person.FirstName),
                ("last_name", Faker.Person.LastName),
                ("title", "Former Employee"),
                ("role", nameof(UserRole.Member)),
                ("email_confirmed", true),
                ("avatar", JsonSerializer.Serialize(new Avatar())),
                ("locale", "en-US"),
                ("external_identities", "[]")
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/api/account/users/{deletedUserId}/purge");

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();
        Connection.RowExists("users", deletedUserId.ToString()).Should().BeFalse();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("UserPurged");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.reason"].Should().Be(nameof(UserPurgeReason.SingleUserPurge));
    }

    [Fact]
    public async Task PurgeUser_WhenMember_ShouldReturnForbidden()
    {
        // Arrange
        var deletedUserId = UserId.NewId();

        // Act
        var response = await AuthenticatedMemberHttpClient.DeleteAsync($"/api/account/users/{deletedUserId}/purge");

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only owners and admins can permanently delete users.");
    }

    [Fact]
    public async Task PurgeUser_WhenUserNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentUserId = UserId.NewId();

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/api/account/users/{nonExistentUserId}/purge");

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"Deleted user with id '{nonExistentUserId}' not found.");
    }

    [Fact]
    public async Task PurgeUser_WhenUserNotDeleted_ShouldReturnNotFound()
    {
        // Arrange
        var activeUserId = UserId.NewId();
        Connection.Insert("users", [
                ("tenant_id", DatabaseSeeder.Tenant1.Id.ToString()),
                ("id", activeUserId.ToString()),
                ("created_at", TimeProvider.GetUtcNow().AddDays(-10)),
                ("modified_at", null),
                ("deleted_at", null),
                ("email", Faker.Internet.UniqueEmail()),
                ("first_name", Faker.Person.FirstName),
                ("last_name", Faker.Person.LastName),
                ("title", "Active Employee"),
                ("role", nameof(UserRole.Member)),
                ("email_confirmed", true),
                ("avatar", JsonSerializer.Serialize(new Avatar())),
                ("locale", "en-US"),
                ("external_identities", "[]")
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/api/account/users/{activeUserId}/purge");

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"Deleted user with id '{activeUserId}' not found.");
    }
}
