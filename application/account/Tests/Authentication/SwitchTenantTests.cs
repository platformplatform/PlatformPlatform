using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Account.Database;
using Account.Features.Authentication.Commands;
using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using Account.Features.Users.Domain;
using FluentAssertions;
using SharedKernel.Domain;
using SharedKernel.Tests;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Authentication;

public sealed class SwitchTenantTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task SwitchTenant_WhenUserExistsInTargetTenant_ShouldSwitchSuccessfully()
    {
        // Arrange
        var tenant2Id = TenantId.NewId();
        var tenant2Name = Faker.Company.CompanyName();
        var user2Id = UserId.NewId();

        Connection.Insert("tenants", [
                ("id", tenant2Id.Value),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("name", tenant2Name),
                ("state", nameof(TenantState.Active)),
                ("logo", """{"Url":null,"Version":0}"""),
                ("plan", nameof(SubscriptionPlan.Basis))
            ]
        );

        InsertSubscription(tenant2Id);

        Connection.Insert("users", [
                ("tenant_id", tenant2Id.Value),
                ("id", user2Id.ToString()),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("email", DatabaseSeeder.Tenant1Member.Email),
                ("email_confirmed", true),
                ("first_name", Faker.Name.FirstName()),
                ("last_name", Faker.Name.LastName()),
                ("title", null),
                ("avatar", JsonSerializer.Serialize(new Avatar())),
                ("role", nameof(UserRole.Member)),
                ("locale", "en-US"),
                ("external_identities", "[]")
            ]
        );

        var command = new SwitchTenantCommand(tenant2Id);

        // Act
        var response = await AuthenticatedMemberHttpClient.PostAsJsonAsync(
            "/api/account/authentication/switch-tenant", command
        );

        // Assert
        await response.ShouldBeSuccessfulPostRequest(hasLocation: false);
        response.Headers.Count(h => h.Key == "x-refresh-token").Should().Be(1);
        response.Headers.Count(h => h.Key == "x-access-token").Should().Be(1);

        var oldSessionRevokedReason = Connection.ExecuteScalar<string>(
            "SELECT revoked_reason FROM sessions WHERE id = @Id",
            [new { Id = DatabaseSeeder.Tenant1MemberSession.Id.ToString() }]
        );
        oldSessionRevokedReason.Should().Be("SwitchTenant");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(3);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("SessionRevoked");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.reason"].Should().Be("SwitchTenant");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("SessionCreated");
        TelemetryEventsCollectorSpy.CollectedEvents[2].GetType().Name.Should().Be("TenantSwitched");
        TelemetryEventsCollectorSpy.CollectedEvents[2].Properties["event.from_tenant_id"].Should().Be(DatabaseSeeder.Tenant1.Id.ToString());
        TelemetryEventsCollectorSpy.CollectedEvents[2].Properties["event.to_tenant_id"].Should().Be(tenant2Id.ToString());
        TelemetryEventsCollectorSpy.CollectedEvents[2].Properties["event.user_id"].Should().Be(user2Id.ToString());
    }

    [Fact]
    public async Task SwitchTenant_WhenTargetTenantDoesNotExist_ShouldReturnForbidden()
    {
        // Arrange
        var nonExistentTenantId = TenantId.NewId();
        var command = new SwitchTenantCommand(nonExistentTenantId);

        // Act
        var response = await AuthenticatedMemberHttpClient.PostAsJsonAsync(
            "/api/account/authentication/switch-tenant", command
        );

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, $"User does not have access to tenant '{nonExistentTenantId}'.");
        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task SwitchTenant_WhenUserDoesNotExistInTargetTenant_ShouldReturnForbidden()
    {
        // Arrange
        var tenant2Id = TenantId.NewId();

        Connection.Insert("tenants", [
                ("id", tenant2Id.Value),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("name", Faker.Company.CompanyName()),
                ("state", nameof(TenantState.Active)),
                ("logo", """{"Url":null,"Version":0}"""),
                ("plan", nameof(SubscriptionPlan.Basis))
            ]
        );

        Connection.Insert("users", [
                ("tenant_id", tenant2Id.Value),
                ("id", UserId.NewId().ToString()),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("email", Faker.Internet.UniqueEmail()),
                ("email_confirmed", true),
                ("first_name", Faker.Name.FirstName()),
                ("last_name", Faker.Name.LastName()),
                ("title", null),
                ("avatar", JsonSerializer.Serialize(new Avatar())),
                ("role", nameof(UserRole.Owner)),
                ("locale", "en-US"),
                ("external_identities", "[]")
            ]
        );

        var command = new SwitchTenantCommand(tenant2Id);

        // Act
        var response = await AuthenticatedMemberHttpClient.PostAsJsonAsync(
            "/api/account/authentication/switch-tenant", command
        );

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, $"User does not have access to tenant '{tenant2Id}'.");
        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task SwitchTenant_WhenUnauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var command = new SwitchTenantCommand(TenantId.NewId());
        var response = await AnonymousHttpClient.PostAsJsonAsync(
            "/api/account/authentication/switch-tenant", command
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SwitchTenant_WhenUserEmailNotConfirmed_ShouldConfirmEmail()
    {
        // Arrange
        var tenant2Id = TenantId.NewId();
        var tenant2Name = Faker.Company.CompanyName();
        var user2Id = UserId.NewId();

        Connection.Insert("tenants", [
                ("id", tenant2Id.Value),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("name", tenant2Name),
                ("state", nameof(TenantState.Active)),
                ("logo", """{"Url":null,"Version":0}"""),
                ("plan", nameof(SubscriptionPlan.Basis))
            ]
        );

        InsertSubscription(tenant2Id);

        Connection.Insert("users", [
                ("tenant_id", tenant2Id.Value),
                ("id", user2Id.ToString()),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("email", DatabaseSeeder.Tenant1Member.Email),
                ("email_confirmed", false), // User's email is not confirmed
                ("first_name", Faker.Name.FirstName()),
                ("last_name", Faker.Name.LastName()),
                ("title", null),
                ("avatar", JsonSerializer.Serialize(new Avatar())),
                ("role", nameof(UserRole.Member)),
                ("locale", "en-US"),
                ("external_identities", "[]")
            ]
        );

        var command = new SwitchTenantCommand(tenant2Id);

        // Act
        var response = await AuthenticatedMemberHttpClient.PostAsJsonAsync(
            "/api/account/authentication/switch-tenant", command
        );

        // Assert
        await response.ShouldBeSuccessfulPostRequest(hasLocation: false);

        // Verify that the user's email is now confirmed
        var emailConfirmed = Connection.ExecuteScalar<long>(
            "SELECT email_confirmed FROM users WHERE id = @Id",
            [new { Id = user2Id.ToString() }]
        );
        emailConfirmed.Should().Be(1); // SQLite stores boolean as 0/1
    }

    [Fact]
    public async Task SwitchTenant_WhenAcceptingInvite_ShouldCopyProfileData()
    {
        // Arrange
        var tenant2Id = TenantId.NewId();
        var tenant2Name = Faker.Company.CompanyName();
        var user2Id = UserId.NewId();

        // Current user has profile data
        var currentFirstName = Faker.Name.FirstName();
        var currentLastName = Faker.Name.LastName();
        var currentTitle = Faker.Name.JobTitle();
        var currentLocale = "da-DK";

        // Update current user with profile data
        Connection.Update("users", "id", DatabaseSeeder.Tenant1Member.Id.ToString(), [
                ("first_name", currentFirstName),
                ("last_name", currentLastName),
                ("title", currentTitle),
                ("locale", currentLocale)
            ]
        );

        Connection.Insert("tenants", [
                ("id", tenant2Id.Value),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("name", tenant2Name),
                ("state", nameof(TenantState.Active)),
                ("logo", """{"Url":null,"Version":0}"""),
                ("plan", nameof(SubscriptionPlan.Basis))
            ]
        );

        InsertSubscription(tenant2Id);

        // New user has no profile data and unconfirmed email
        Connection.Insert("users", [
                ("tenant_id", tenant2Id.Value),
                ("id", user2Id.ToString()),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("email", DatabaseSeeder.Tenant1Member.Email),
                ("email_confirmed", false), // Unconfirmed - invitation pending
                ("first_name", null),
                ("last_name", null),
                ("title", "Manager"), // Has a title that will be overwritten
                ("avatar", JsonSerializer.Serialize(new Avatar())),
                ("role", nameof(UserRole.Member)),
                ("locale", "en-US"),
                ("external_identities", "[]")
            ]
        );

        var command = new SwitchTenantCommand(tenant2Id);

        // Act
        var response = await AuthenticatedMemberHttpClient.PostAsJsonAsync(
            "/api/account/authentication/switch-tenant", command
        );

        // Assert
        await response.ShouldBeSuccessfulPostRequest(hasLocation: false);

        // Verify profile data was copied
        var firstName = Connection.ExecuteScalar<string>(
            "SELECT first_name FROM users WHERE id = @Id",
            [new { Id = user2Id.ToString() }]
        );
        var lastName = Connection.ExecuteScalar<string>(
            "SELECT last_name FROM users WHERE id = @Id",
            [new { Id = user2Id.ToString() }]
        );
        var title = Connection.ExecuteScalar<string>(
            "SELECT title FROM users WHERE id = @Id",
            [new { Id = user2Id.ToString() }]
        );
        var locale = Connection.ExecuteScalar<string>(
            "SELECT locale FROM users WHERE id = @Id",
            [new { Id = user2Id.ToString() }]
        );
        var emailConfirmed = Connection.ExecuteScalar<long>(
            "SELECT email_confirmed FROM users WHERE id = @Id",
            [new { Id = user2Id.ToString() }]
        );

        firstName.Should().Be(currentFirstName);
        lastName.Should().Be(currentLastName);
        title.Should().Be(currentTitle);
        locale.Should().Be(currentLocale);

        // Email should be confirmed
        emailConfirmed.Should().Be(1);
    }

    [Fact]
    public async Task SwitchTenant_WhenSessionAlreadyRevoked_ShouldReturnUnauthorized()
    {
        // Arrange
        var tenant2Id = TenantId.NewId();
        var user2Id = UserId.NewId();

        Connection.Insert("tenants", [
                ("id", tenant2Id.Value),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("name", Faker.Company.CompanyName()),
                ("state", nameof(TenantState.Active)),
                ("logo", """{"Url":null,"Version":0}"""),
                ("plan", nameof(SubscriptionPlan.Basis))
            ]
        );

        InsertSubscription(tenant2Id);

        Connection.Insert("users", [
                ("tenant_id", tenant2Id.Value),
                ("id", user2Id.ToString()),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("email", DatabaseSeeder.Tenant1Member.Email),
                ("email_confirmed", true),
                ("first_name", Faker.Name.FirstName()),
                ("last_name", Faker.Name.LastName()),
                ("title", null),
                ("avatar", JsonSerializer.Serialize(new Avatar())),
                ("role", nameof(UserRole.Member)),
                ("locale", "en-US"),
                ("external_identities", "[]")
            ]
        );

        // First switch succeeds and revokes the current session
        var response1 = await AuthenticatedMemberHttpClient.PostAsJsonAsync(
            "/api/account/authentication/switch-tenant", new SwitchTenantCommand(tenant2Id)
        );
        await response1.ShouldBeSuccessfulPostRequest(hasLocation: false);
        TelemetryEventsCollectorSpy.Reset();

        // Act - Attempt to switch again with the same (now revoked) session
        var response2 = await AuthenticatedMemberHttpClient.PostAsJsonAsync(
            "/api/account/authentication/switch-tenant", new SwitchTenantCommand(DatabaseSeeder.Tenant1.Id)
        );

        // Assert
        await response2.ShouldHaveErrorStatusCode(HttpStatusCode.Unauthorized, "Session has been revoked.");
        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    private void InsertSubscription(TenantId tenantId)
    {
        Connection.Insert("subscriptions", [
                ("tenant_id", tenantId.Value),
                ("id", SubscriptionId.NewId().ToString()),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("plan", nameof(SubscriptionPlan.Basis)),
                ("scheduled_plan", null),
                ("stripe_customer_id", null),
                ("stripe_subscription_id", null),
                ("current_price_amount", null),
                ("current_price_currency", null),
                ("current_period_end", null),
                ("cancel_at_period_end", false),
                ("first_payment_failed_at", null),
                ("cancellation_reason", null),
                ("cancellation_feedback", null),
                ("payment_transactions", "[]"),
                ("payment_method", null),
                ("billing_info", null)
            ]
        );
    }
}
