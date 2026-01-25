using System.Text.Json;
using FluentAssertions;
using PlatformPlatform.Account.Database;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.Account.Features.Users.Queries;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using Xunit;

namespace PlatformPlatform.Account.Tests.Users;

public sealed class GetUserSummaryTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task GetUserSummary_WhenUsersHaveVariousLastSeenDates_ShouldCountActiveUsersCorrectly()
    {
        // Arrange
        var now = TimeProvider.GetUtcNow();
        var thirtyOneDaysAgo = now.AddDays(-31);

        // Set the seeded owner user as active (LastSeenAt within 30 days)
        Connection.Update("Users", "Id", DatabaseSeeder.Tenant1Owner.Id.ToString(), [("LastSeenAt", now)]);

        // Insert an active user (LastSeenAt within 30 days, confirmed)
        Connection.Insert("Users", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", UserId.NewId().ToString()),
                ("CreatedAt", now.AddMinutes(-10)),
                ("ModifiedAt", null),
                ("Email", "active@example.com"),
                ("FirstName", "Active"),
                ("LastName", "User"),
                ("Title", null),
                ("Role", nameof(UserRole.Member)),
                ("EmailConfirmed", true),
                ("LastSeenAt", now.AddDays(-5)),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Locale", "en-US"),
                ("ExternalIdentities", "[]")
            ]
        );

        // Insert an inactive user (LastSeenAt older than 30 days, confirmed)
        Connection.Insert("Users", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", UserId.NewId().ToString()),
                ("CreatedAt", now.AddDays(-60)),
                ("ModifiedAt", null),
                ("Email", "inactive@example.com"),
                ("FirstName", "Inactive"),
                ("LastName", "User"),
                ("Title", null),
                ("Role", nameof(UserRole.Member)),
                ("EmailConfirmed", true),
                ("LastSeenAt", thirtyOneDaysAgo),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Locale", "en-US"),
                ("ExternalIdentities", "[]")
            ]
        );

        // Insert a pending user (not confirmed, no LastSeenAt)
        Connection.Insert("Users", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", UserId.NewId().ToString()),
                ("CreatedAt", now.AddMinutes(-5)),
                ("ModifiedAt", null),
                ("Email", "pending@example.com"),
                ("FirstName", null),
                ("LastName", null),
                ("Title", null),
                ("Role", nameof(UserRole.Member)),
                ("EmailConfirmed", false),
                ("LastSeenAt", null),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Locale", "en-US"),
                ("ExternalIdentities", "[]")
            ]
        );

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account/users/summary");

        // Assert
        response.ShouldBeSuccessfulGetRequest();
        var summaryResponse = await response.DeserializeResponse<UserSummaryResponse>();
        summaryResponse.Should().NotBeNull();
        summaryResponse.TotalUsers.Should().Be(5);
        summaryResponse.ActiveUsers.Should().Be(2);
        summaryResponse.PendingUsers.Should().Be(1);
    }
}
