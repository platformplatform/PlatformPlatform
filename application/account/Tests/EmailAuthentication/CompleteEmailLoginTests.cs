using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Account.Database;
using Account.Features.EmailAuthentication.Commands;
using Account.Features.EmailAuthentication.Domain;
using Account.Features.Subscriptions.Domain;
using Account.Features.Tenants.Domain;
using Account.Features.Users.Commands;
using Account.Features.Users.Domain;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using SharedKernel.Domain;
using SharedKernel.Tests;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.EmailAuthentication;

public sealed class CompleteEmailLoginTests : EndpointBaseTest<AccountDbContext>
{
    private const string CorrectOneTimePassword = "UNLOCK"; // UNLOCK is a special global OTP for development and tests
    private const string WrongOneTimePassword = "FAULTY";

    [Fact]
    public async Task CompleteEmailLogin_WhenValid_ShouldCompleteEmailLoginAndCreateTokens()
    {
        // Arrange
        var emailLoginId = await StartEmailLogin(DatabaseSeeder.Tenant1Owner.Email);
        var command = new CompleteEmailLoginCommand(CorrectOneTimePassword);

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account/authentication/email/login/{emailLoginId}/complete", command);

        // Assert
        await response.ShouldBeSuccessfulPostRequest(hasLocation: false);
        var updatedEmailLoginCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM email_logins WHERE id = @id AND completed = 1", [new { id = emailLoginId.ToString() }]
        );
        updatedEmailLoginCount.Should().Be(1);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(3);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("EmailLoginStarted");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("SessionCreated");
        TelemetryEventsCollectorSpy.CollectedEvents[2].GetType().Name.Should().Be("EmailLoginCompleted");
        TelemetryEventsCollectorSpy.CollectedEvents[2].Properties["event.user_id"].Should().Be(DatabaseSeeder.Tenant1Owner.Id);
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();

        response.Headers.Count(h => h.Key == "x-refresh-token").Should().Be(1);
        response.Headers.Count(h => h.Key == "x-access-token").Should().Be(1);
    }

    [Fact]
    public async Task CompleteEmailLogin_WhenEmailLoginNotFound_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidEmailLoginId = EmailLoginId.NewId();
        var command = new CompleteEmailLoginCommand(CorrectOneTimePassword);

        // Act
        var response = await AnonymousHttpClient.PostAsJsonAsync($"/api/account/authentication/email/login/{invalidEmailLoginId}/complete", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"Email login with id '{invalidEmailLoginId}' not found.");

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task CompleteEmailLogin_WhenInvalidOneTimePassword_ShouldReturnBadRequest()
    {
        // Arrange
        var emailLoginId = await StartEmailLogin(DatabaseSeeder.Tenant1Owner.Email);
        var command = new CompleteEmailLoginCommand(WrongOneTimePassword);

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account/authentication/email/login/{emailLoginId}/complete", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, "The code is wrong or no longer valid.");

        var updatedRetryCount = Connection.ExecuteScalar<long>("SELECT retry_count FROM email_logins WHERE id = @id", [new { id = emailLoginId.ToString() }]);
        updatedRetryCount.Should().Be(1);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(2);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("EmailLoginStarted");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("EmailLoginCodeFailed");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteEmailLogin_WhenEmailLoginAlreadyCompleted_ShouldReturnBadRequest()
    {
        // Arrange
        var emailLoginId = await StartEmailLogin(DatabaseSeeder.Tenant1Owner.Email);
        var command = new CompleteEmailLoginCommand(CorrectOneTimePassword);
        await AnonymousHttpClient.PostAsJsonAsync($"/api/account/authentication/email/login/{emailLoginId}/complete", command);

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account/authentication/email/login/{emailLoginId}/complete", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(
            HttpStatusCode.BadRequest, $"Email login with id '{emailLoginId}' has already been completed."
        );
    }

    [Fact]
    public async Task CompleteEmailLogin_WhenRetryCountExceeded_ShouldReturnForbidden()
    {
        // Arrange
        var emailLoginId = await StartEmailLogin(DatabaseSeeder.Tenant1Owner.Email);
        var command = new CompleteEmailLoginCommand(WrongOneTimePassword);
        await AnonymousHttpClient.PostAsJsonAsync($"/api/account/authentication/email/login/{emailLoginId}/complete", command);
        await AnonymousHttpClient.PostAsJsonAsync($"/api/account/authentication/email/login/{emailLoginId}/complete", command);
        await AnonymousHttpClient.PostAsJsonAsync($"/api/account/authentication/email/login/{emailLoginId}/complete", command);

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account/authentication/email/login/{emailLoginId}/complete", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Too many attempts, please request a new code.");

        var updatedRetryCount = Connection.ExecuteScalar<long>(
            "SELECT retry_count FROM email_logins WHERE id = @id", [new { id = emailLoginId.ToString() }]
        );
        updatedRetryCount.Should().Be(4);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(5);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("EmailLoginStarted");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("EmailLoginCodeFailed");
        TelemetryEventsCollectorSpy.CollectedEvents[2].GetType().Name.Should().Be("EmailLoginCodeFailed");
        TelemetryEventsCollectorSpy.CollectedEvents[3].GetType().Name.Should().Be("EmailLoginCodeFailed");
        TelemetryEventsCollectorSpy.CollectedEvents[4].GetType().Name.Should().Be("EmailLoginCodeBlocked");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteEmailLogin_WhenEmailLoginExpired_ShouldReturnBadRequest()
    {
        // Arrange
        var emailLoginId = EmailLoginId.NewId();

        Connection.Insert("email_logins", [
                ("id", emailLoginId.ToString()),
                ("created_at", TimeProvider.GetUtcNow().AddMinutes(-10)),
                ("modified_at", null),
                ("email", DatabaseSeeder.Tenant1Owner.Email),
                ("type", nameof(EmailLoginType.Login)),
                ("one_time_password_hash", new PasswordHasher<object>().HashPassword(this, CorrectOneTimePassword)),
                ("retry_count", 0),
                ("resend_count", 0),
                ("completed", false)
            ]
        );

        var command = new CompleteEmailLoginCommand(CorrectOneTimePassword);

        // Act
        var response = await AnonymousHttpClient.PostAsJsonAsync($"/api/account/authentication/email/login/{emailLoginId}/complete", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, "The code is no longer valid, please request a new code.");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("EmailLoginCodeExpired");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteEmailLogin_WhenUserInviteCompleted_ShouldTrackUserInviteAcceptedEvent()
    {
        // Arrange
        Connection.Update("tenants", "id", DatabaseSeeder.Tenant1.Id.ToString(), [("name", "Test Company")]);

        var email = Faker.Internet.UniqueEmail();
        var inviteUserCommand = new InviteUserCommand(email);
        await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account/users/invite", inviteUserCommand);
        TelemetryEventsCollectorSpy.Reset();

        var emailLoginId = await StartEmailLogin(email);
        var command = new CompleteEmailLoginCommand(CorrectOneTimePassword);

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account/authentication/email/login/{emailLoginId}/complete", command);

        // Assert
        await response.ShouldBeSuccessfulPostRequest(hasLocation: false);
        Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM users WHERE tenant_id = @tenantId AND email = @email AND email_confirmed = 1",
            [new { tenantId = DatabaseSeeder.Tenant1.Id.ToString(), email = email.ToLower() }]
        ).Should().Be(1);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(4);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("EmailLoginStarted");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("UserInviteAccepted");
        TelemetryEventsCollectorSpy.CollectedEvents[2].GetType().Name.Should().Be("SessionCreated");
        TelemetryEventsCollectorSpy.CollectedEvents[3].GetType().Name.Should().Be("EmailLoginCompleted");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteEmailLogin_WithValidPreferredTenant_ShouldLoginToPreferredTenant()
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

        Connection.Insert("subscriptions", [
                ("tenant_id", tenant2Id.Value),
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

        Connection.Insert("users", [
                ("tenant_id", tenant2Id.Value),
                ("id", user2Id.ToString()),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("email", DatabaseSeeder.Tenant1Owner.Email),
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

        var emailLoginId = await StartEmailLogin(DatabaseSeeder.Tenant1Owner.Email);
        var command = new CompleteEmailLoginCommand(CorrectOneTimePassword, tenant2Id);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account/authentication/email/login/{emailLoginId}/complete", command);

        // Assert
        await response.ShouldBeSuccessfulPostRequest(hasLocation: false);
        response.Headers.Count(h => h.Key == "x-refresh-token").Should().Be(1);
        response.Headers.Count(h => h.Key == "x-access-token").Should().Be(1);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(2);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("SessionCreated");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("EmailLoginCompleted");
        TelemetryEventsCollectorSpy.CollectedEvents[1].Properties["event.user_id"].Should().Be(user2Id);
    }

    [Fact]
    public async Task CompleteEmailLogin_WithInvalidPreferredTenant_ShouldLoginToDefaultTenant()
    {
        // Arrange
        var invalidTenantId = TenantId.NewId();
        var emailLoginId = await StartEmailLogin(DatabaseSeeder.Tenant1Owner.Email);
        var command = new CompleteEmailLoginCommand(CorrectOneTimePassword, invalidTenantId);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account/authentication/email/login/{emailLoginId}/complete", command);

        // Assert
        await response.ShouldBeSuccessfulPostRequest(hasLocation: false);
        response.Headers.Count(h => h.Key == "x-refresh-token").Should().Be(1);
        response.Headers.Count(h => h.Key == "x-access-token").Should().Be(1);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(2);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("SessionCreated");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("EmailLoginCompleted");
        TelemetryEventsCollectorSpy.CollectedEvents[1].Properties["event.user_id"].Should().Be(DatabaseSeeder.Tenant1Owner.Id);
    }

    [Fact]
    public async Task CompleteEmailLogin_WithPreferredTenantUserDoesNotHaveAccess_ShouldLoginToDefaultTenant()
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

        var emailLoginId = await StartEmailLogin(DatabaseSeeder.Tenant1Owner.Email);
        var command = new CompleteEmailLoginCommand(CorrectOneTimePassword, tenant2Id);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account/authentication/email/login/{emailLoginId}/complete", command);

        // Assert
        await response.ShouldBeSuccessfulPostRequest(hasLocation: false);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(2);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("SessionCreated");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("EmailLoginCompleted");
        TelemetryEventsCollectorSpy.CollectedEvents[1].Properties["event.user_id"].Should().Be(DatabaseSeeder.Tenant1Owner.Id);
    }

    private async Task<EmailLoginId> StartEmailLogin(string email)
    {
        var command = new StartEmailLoginCommand(email);
        var response = await AnonymousHttpClient.PostAsJsonAsync("/api/account/authentication/email/login/start", command);
        var responseBody = await response.DeserializeResponse<StartEmailLoginResponse>();
        return responseBody!.EmailLoginId;
    }
}
