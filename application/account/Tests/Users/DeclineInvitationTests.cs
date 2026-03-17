using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Account.Database;
using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using Account.Features.Users.Commands;
using Account.Features.Users.Domain;
using FluentAssertions;
using SharedKernel.Domain;
using SharedKernel.Tests;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Users;

public sealed class DeclineInvitationTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task DeclineInvitation_WhenValidInviteExists_ShouldDeleteUserAndCollectTelemetry()
    {
        // Arrange
        var newTenantId = TenantId.NewId();
        var userId = UserId.NewId();

        Connection.Insert("tenants", [
                ("id", newTenantId.ToString()),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("name", Faker.Company.CompanyName()),
                ("state", nameof(TenantState.Active)),
                ("logo", """{"Url":null,"Version":0}"""),
                ("plan", nameof(SubscriptionPlan.Basis))
            ]
        );

        Connection.Insert("users", [
                ("tenant_id", newTenantId.ToString()),
                ("id", userId.ToString()),
                ("created_at", TimeProvider.GetUtcNow().AddMinutes(-10)),
                ("modified_at", null),
                ("deleted_at", null),
                ("email", DatabaseSeeder.Tenant1Member.Email),
                ("email_confirmed", false),
                ("first_name", null),
                ("last_name", null),
                ("title", null),
                ("avatar", JsonSerializer.Serialize(new Avatar())),
                ("role", nameof(UserRole.Member)),
                ("locale", ""),
                ("external_identities", "[]")
            ]
        );

        // Act
        var response = await AuthenticatedMemberHttpClient.PostAsJsonAsync(
            "/api/account/users/decline-invitation",
            new DeclineInvitationCommand(newTenantId)
        );

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();
        Connection.RowExists("users", userId.ToString()).Should().BeTrue();
        var deletedAt = Connection.ExecuteScalar<string>("SELECT deleted_at FROM users WHERE id = @id", [new { id = userId.ToString() }]);
        deletedAt.Should().NotBeNullOrEmpty();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(2);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("UserInviteDeclined");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("UserDeleted");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task DeclineInvitation_WhenInvitationNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var nonExistentTenantId = TenantId.NewId();

        // Act
        var response = await AuthenticatedMemberHttpClient.PostAsJsonAsync(
            "/api/account/users/decline-invitation",
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

        Connection.Insert("tenants", [
                ("id", tenant2Id.ToString()),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("name", Faker.Company.CompanyName()),
                ("state", nameof(TenantState.Active)),
                ("logo", """{"Url":null,"Version":0}"""),
                ("plan", nameof(SubscriptionPlan.Basis))
            ]
        );

        Connection.Insert("tenants", [
                ("id", tenant3Id.ToString()),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("name", Faker.Company.CompanyName()),
                ("state", nameof(TenantState.Active)),
                ("logo", """{"Url":null,"Version":0}"""),
                ("plan", nameof(SubscriptionPlan.Basis))
            ]
        );

        Connection.Insert("users", [
                ("tenant_id", tenant2Id.ToString()),
                ("id", userId2.ToString()),
                ("created_at", TimeProvider.GetUtcNow().AddMinutes(-10)),
                ("modified_at", null),
                ("deleted_at", null),
                ("email", DatabaseSeeder.Tenant1Member.Email),
                ("email_confirmed", false),
                ("first_name", null),
                ("last_name", null),
                ("title", null),
                ("avatar", JsonSerializer.Serialize(new Avatar())),
                ("role", nameof(UserRole.Member)),
                ("locale", ""),
                ("external_identities", "[]")
            ]
        );

        Connection.Insert("users", [
                ("tenant_id", tenant3Id.ToString()),
                ("id", userId3.ToString()),
                ("created_at", TimeProvider.GetUtcNow().AddMinutes(-5)),
                ("modified_at", null),
                ("deleted_at", null),
                ("email", DatabaseSeeder.Tenant1Member.Email),
                ("email_confirmed", false),
                ("first_name", null),
                ("last_name", null),
                ("title", null),
                ("avatar", JsonSerializer.Serialize(new Avatar())),
                ("role", nameof(UserRole.Member)),
                ("locale", ""),
                ("external_identities", "[]")
            ]
        );

        // Act
        var response = await AuthenticatedMemberHttpClient.PostAsJsonAsync(
            "/api/account/users/decline-invitation",
            new DeclineInvitationCommand(tenant2Id)
        );

        // Assert
        response.ShouldHaveEmptyHeaderAndLocationOnSuccess();
        Connection.RowExists("users", userId2.ToString()).Should().BeTrue();
        var deletedAt = Connection.ExecuteScalar<string>("SELECT deleted_at FROM users WHERE id = @id", [new { id = userId2.ToString() }]);
        deletedAt.Should().NotBeNullOrEmpty();
        Connection.RowExists("users", userId3.ToString()).Should().BeTrue();

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(2);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("UserInviteDeclined");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("UserDeleted");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task DeclineInvitation_WhenInvitationAlreadyAccepted_ShouldReturnBadRequest()
    {
        // Act
        var response = await AuthenticatedMemberHttpClient.PostAsJsonAsync(
            "/api/account/users/decline-invitation",
            new DeclineInvitationCommand(DatabaseSeeder.Tenant1.Id)
        );

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, "This invitation has already been accepted.");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }
}
