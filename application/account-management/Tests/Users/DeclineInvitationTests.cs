using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.Tenants.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Commands;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Users;

public sealed class DeclineInvitationTests : EndpointBaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task DeclineInvitation_WhenValidInviteExists_ShouldDeleteUserAndCollectTelemetry()
    {
        // Arrange
        var newTenantId = TenantId.NewId();
        var userId = UserId.NewId();

        Connection.Insert("Tenants", [
                ("Id", newTenantId.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow()),
                ("ModifiedAt", null),
                ("Name", Faker.Company.CompanyName()),
                ("State", TenantState.Trial.ToString()),
                ("Logo", """{"Url":null,"Version":0}""")
            ]
        );

        Connection.Insert("Users", [
                ("TenantId", newTenantId.ToString()),
                ("Id", userId.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow().AddMinutes(-10)),
                ("ModifiedAt", null),
                ("Email", DatabaseSeeder.Tenant1Member.Email),
                ("EmailConfirmed", false),
                ("FirstName", null),
                ("LastName", null),
                ("Title", null),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Role", UserRole.Member.ToString()),
                ("Locale", "")
            ]
        );

        // Act
        var response = await AuthenticatedMemberHttpClient.PostAsJsonAsync(
            "/api/account-management/users/decline-invitation",
            new DeclineInvitationCommand(newTenantId)
        );

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();
        Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM Users WHERE Id = @userId",
            new { userId = userId.ToString() }
        ).Should().Be(0);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("UserInviteDeclined");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task DeclineInvitation_WhenInvitationNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentTenantId = TenantId.NewId();

        // Act
        var response = await AuthenticatedMemberHttpClient.PostAsJsonAsync(
            "/api/account-management/users/decline-invitation",
            new DeclineInvitationCommand(nonExistentTenantId)
        );

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, "The invitation has been revoked.");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task DeclineInvitation_WhenMultipleInvitesExist_ShouldDeclineSpecificOne()
    {
        // Arrange
        var tenant2Id = TenantId.NewId();
        var tenant3Id = TenantId.NewId();
        var userId2 = UserId.NewId();
        var userId3 = UserId.NewId();

        Connection.Insert("Tenants", [
                ("Id", tenant2Id.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow()),
                ("ModifiedAt", null),
                ("Name", Faker.Company.CompanyName()),
                ("State", TenantState.Trial.ToString()),
                ("Logo", """{"Url":null,"Version":0}""")
            ]
        );

        Connection.Insert("Tenants", [
                ("Id", tenant3Id.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow()),
                ("ModifiedAt", null),
                ("Name", Faker.Company.CompanyName()),
                ("State", TenantState.Trial.ToString()),
                ("Logo", """{"Url":null,"Version":0}""")
            ]
        );

        Connection.Insert("Users", [
                ("TenantId", tenant2Id.ToString()),
                ("Id", userId2.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow().AddMinutes(-10)),
                ("ModifiedAt", null),
                ("Email", DatabaseSeeder.Tenant1Member.Email),
                ("EmailConfirmed", false),
                ("FirstName", null),
                ("LastName", null),
                ("Title", null),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Role", UserRole.Member.ToString()),
                ("Locale", "")
            ]
        );

        Connection.Insert("Users", [
                ("TenantId", tenant3Id.ToString()),
                ("Id", userId3.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow().AddMinutes(-5)),
                ("ModifiedAt", null),
                ("Email", DatabaseSeeder.Tenant1Member.Email),
                ("EmailConfirmed", false),
                ("FirstName", null),
                ("LastName", null),
                ("Title", null),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Role", UserRole.Member.ToString()),
                ("Locale", "")
            ]
        );

        // Act
        var response = await AuthenticatedMemberHttpClient.PostAsJsonAsync(
            "/api/account-management/users/decline-invitation",
            new DeclineInvitationCommand(tenant2Id)
        );

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();
        Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM Users WHERE Id = @userId",
            new { userId = userId2.ToString() }
        ).Should().Be(0);
        Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM Users WHERE Id = @userId",
            new { userId = userId3.ToString() }
        ).Should().Be(1);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("UserInviteDeclined");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task DeclineInvitation_WhenInvitationAlreadyAccepted_ShouldReturnBadRequest()
    {
        // Act
        var response = await AuthenticatedMemberHttpClient.PostAsJsonAsync(
            "/api/account-management/users/decline-invitation",
            new DeclineInvitationCommand(DatabaseSeeder.Tenant1.Id)
        );

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, "This invitation has already been accepted.");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }
}
