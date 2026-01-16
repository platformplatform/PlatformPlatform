using System.Net;
using System.Text.Json;
using FluentAssertions;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Users;

public sealed class PurgeUserTests : EndpointBaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task PurgeUser_WhenOwnerDeletesSoftDeletedUser_ShouldSucceed()
    {
        // Arrange
        var deletedUserId = UserId.NewId();
        Connection.Insert("Users", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", deletedUserId.ToString()),
                ("CreatedAt", TimeProvider.GetUtcNow().AddDays(-10)),
                ("ModifiedAt", TimeProvider.GetUtcNow().AddDays(-1)),
                ("DeletedAt", TimeProvider.GetUtcNow().AddDays(-1)),
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
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/api/account-management/users/{deletedUserId}/purge");

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();
        Connection.RowExists("Users", deletedUserId.ToString()).Should().BeFalse();

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
        var response = await AuthenticatedMemberHttpClient.DeleteAsync($"/api/account-management/users/{deletedUserId}/purge");

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Only owners and admins can permanently delete users.");
    }

    [Fact]
    public async Task PurgeUser_WhenUserNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentUserId = UserId.NewId();

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/api/account-management/users/{nonExistentUserId}/purge");

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"Deleted user with id '{nonExistentUserId}' not found.");
    }

    [Fact]
    public async Task PurgeUser_WhenUserNotDeleted_ShouldReturnNotFound()
    {
        // Arrange
        var activeUserId = UserId.NewId();
        Connection.Insert("Users", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", activeUserId.ToString()),
                ("CreatedAt", TimeProvider.GetUtcNow().AddDays(-10)),
                ("ModifiedAt", null),
                ("DeletedAt", null),
                ("Email", Faker.Internet.UniqueEmail()),
                ("FirstName", Faker.Person.FirstName),
                ("LastName", Faker.Person.LastName),
                ("Title", "Active Employee"),
                ("Role", nameof(UserRole.Member)),
                ("EmailConfirmed", true),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Locale", "en-US"),
                ("ExternalIdentities", "[]")
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.DeleteAsync($"/api/account-management/users/{activeUserId}/purge");

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"Deleted user with id '{activeUserId}' not found.");
    }
}
