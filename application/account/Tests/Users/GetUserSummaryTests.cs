using System.Text.Json;
using Account.Database;
using Account.Features.Users.Domain;
using Account.Features.Users.Queries;
using FluentAssertions;
using SharedKernel.Domain;
using SharedKernel.Tests;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Users;

public sealed class GetUserSummaryTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task GetUserSummary_WhenUsersHaveVariousLastSeenDates_ShouldCountActiveUsersCorrectly()
    {
        // Arrange
        var now = TimeProvider.GetUtcNow();
        var thirtyOneDaysAgo = now.AddDays(-31);

        // Set the seeded owner user as active (LastSeenAt within 30 days)
        Connection.Update("users", "id", DatabaseSeeder.Tenant1Owner.Id.ToString(), [("last_seen_at", now)]);

        // Insert an active user (LastSeenAt within 30 days, confirmed)
        Connection.Insert("users", [
                ("tenant_id", DatabaseSeeder.Tenant1.Id.ToString()),
                ("id", UserId.NewId().ToString()),
                ("created_at", now.AddMinutes(-10)),
                ("modified_at", null),
                ("email", "active@example.com"),
                ("first_name", "Active"),
                ("last_name", "User"),
                ("title", null),
                ("role", nameof(UserRole.Member)),
                ("email_confirmed", true),
                ("last_seen_at", now.AddDays(-5)),
                ("avatar", JsonSerializer.Serialize(new Avatar())),
                ("locale", "en-US"),
                ("external_identities", "[]")
            ]
        );

        // Insert an inactive user (LastSeenAt older than 30 days, confirmed)
        Connection.Insert("users", [
                ("tenant_id", DatabaseSeeder.Tenant1.Id.ToString()),
                ("id", UserId.NewId().ToString()),
                ("created_at", now.AddDays(-60)),
                ("modified_at", null),
                ("email", "inactive@example.com"),
                ("first_name", "Inactive"),
                ("last_name", "User"),
                ("title", null),
                ("role", nameof(UserRole.Member)),
                ("email_confirmed", true),
                ("last_seen_at", thirtyOneDaysAgo),
                ("avatar", JsonSerializer.Serialize(new Avatar())),
                ("locale", "en-US"),
                ("external_identities", "[]")
            ]
        );

        // Insert a pending user (not confirmed, no LastSeenAt)
        Connection.Insert("users", [
                ("tenant_id", DatabaseSeeder.Tenant1.Id.ToString()),
                ("id", UserId.NewId().ToString()),
                ("created_at", now.AddMinutes(-5)),
                ("modified_at", null),
                ("email", "pending@example.com"),
                ("first_name", null),
                ("last_name", null),
                ("title", null),
                ("role", nameof(UserRole.Member)),
                ("email_confirmed", false),
                ("last_seen_at", null),
                ("avatar", JsonSerializer.Serialize(new Avatar())),
                ("locale", "en-US"),
                ("external_identities", "[]")
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
