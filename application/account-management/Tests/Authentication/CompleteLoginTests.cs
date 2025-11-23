using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.Authentication.Commands;
using PlatformPlatform.AccountManagement.Features.Authentication.Domain;
using PlatformPlatform.AccountManagement.Features.EmailConfirmations.Domain;
using PlatformPlatform.AccountManagement.Features.Tenants.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Commands;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Authentication;

public sealed class CompleteLoginTests : EndpointBaseTest<AccountManagementDbContext>
{
    private const string CorrectOneTimePassword = "UNLOCK"; // UNLOCK is a special global OTP for development and tests
    private const string WrongOneTimePassword = "FAULTY";

    [Fact]
    public async Task CompleteLogin_WhenValid_ShouldCompleteLoginAndCreateTokens()
    {
        // Arrange
        var (loginId, _) = await StartLogin(DatabaseSeeder.Tenant1Owner.Email);
        var command = new CompleteLoginCommand(CorrectOneTimePassword);

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account-management/authentication/login/{loginId}/complete", command);

        // Assert
        await response.ShouldBeSuccessfulPostRequest(hasLocation: false);
        var updatedLoginCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM Logins WHERE Id = @id AND Completed = 1", [new { id = loginId.ToString() }]
        );
        updatedLoginCount.Should().Be(1);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(2);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("LoginStarted");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("LoginCompleted");
        TelemetryEventsCollectorSpy.CollectedEvents[1].Properties["event.user_id"].Should().Be(DatabaseSeeder.Tenant1Owner.Id);
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();

        response.Headers.Count(h => h.Key == "x-refresh-token").Should().Be(1);
        response.Headers.Count(h => h.Key == "x-access-token").Should().Be(1);
    }

    [Fact]
    public async Task CompleteLogin_WhenLoginNotFound_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidLoginId = LoginId.NewId();
        var command = new CompleteLoginCommand(CorrectOneTimePassword);

        // Act
        var response = await AnonymousHttpClient.PostAsJsonAsync($"/api/account-management/authentication/login/{invalidLoginId}/complete", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, "The code is wrong or no longer valid.");

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task CompleteLogin_WhenInvalidOneTimePassword_ShouldReturnBadRequest()
    {
        // Arrange
        var (loginId, emailConfirmationId) = await StartLogin(DatabaseSeeder.Tenant1Owner.Email);
        var command = new CompleteLoginCommand(WrongOneTimePassword);

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account-management/authentication/login/{loginId}/complete", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, "The code is wrong or no longer valid.");

        // Verify retry count increment and event collection
        var loginCompleted = Connection.ExecuteScalar<long>("SELECT Completed FROM Logins WHERE Id = @id", [new { id = loginId.ToString() }]);
        loginCompleted.Should().Be(0);
        var updatedRetryCount = Connection.ExecuteScalar<long>("SELECT RetryCount FROM EmailConfirmations WHERE Id = @id", [new { id = emailConfirmationId.ToString() }]);
        updatedRetryCount.Should().Be(1);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(2);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("LoginStarted");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("EmailConfirmationFailed");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteLogin_WhenLoginAlreadyCompleted_ShouldReturnBadRequest()
    {
        // Arrange
        var (loginId, _) = await StartLogin(DatabaseSeeder.Tenant1Owner.Email);
        var command = new CompleteLoginCommand(CorrectOneTimePassword);
        await AnonymousHttpClient.PostAsJsonAsync($"/api/account-management/authentication/login/{loginId}/complete", command);

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account-management/authentication/login/{loginId}/complete", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(
            HttpStatusCode.BadRequest, $"The login process '{loginId}' for user '{DatabaseSeeder.Tenant1Owner.Id}' has already been completed."
        );
    }

    [Fact]
    public async Task CompleteLogin_WhenRetryCountExceeded_ShouldReturnForbidden()
    {
        // Arrange
        var (loginId, emailConfirmationId) = await StartLogin(DatabaseSeeder.Tenant1Owner.Email);
        var command = new CompleteLoginCommand(WrongOneTimePassword);
        await AnonymousHttpClient.PostAsJsonAsync($"/api/account-management/authentication/login/{loginId}/complete", command);
        await AnonymousHttpClient.PostAsJsonAsync($"/api/account-management/authentication/login/{loginId}/complete", command);
        await AnonymousHttpClient.PostAsJsonAsync($"/api/account-management/authentication/login/{loginId}/complete", command);

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account-management/authentication/login/{loginId}/complete", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Too many attempts, please request a new code.");

        // Verify retry count increment and event collection
        var loginCompleted = Connection.ExecuteScalar<long>(
            "SELECT Completed FROM Logins WHERE Id = @id", [new { id = loginId.ToString() }]
        );
        loginCompleted.Should().Be(0);
        var updatedRetryCount = Connection.ExecuteScalar<long>(
            "SELECT RetryCount FROM EmailConfirmations WHERE Id = @id", [new { id = emailConfirmationId.ToString() }]
        );
        updatedRetryCount.Should().Be(4);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(5);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("LoginStarted");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("EmailConfirmationFailed");
        TelemetryEventsCollectorSpy.CollectedEvents[2].GetType().Name.Should().Be("EmailConfirmationFailed");
        TelemetryEventsCollectorSpy.CollectedEvents[3].GetType().Name.Should().Be("EmailConfirmationFailed");
        TelemetryEventsCollectorSpy.CollectedEvents[4].GetType().Name.Should().Be("EmailConfirmationBlocked");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteLogin_WhenLoginExpired_ShouldReturnBadRequest()
    {
        // Arrange
        var loginId = LoginId.NewId();
        var emailConfirmationId = EmailConfirmationId.NewId();

        // Insert expired login
        Connection.Insert("Logins", [
                ("TenantId", DatabaseSeeder.Tenant1Owner.TenantId.ToString()),
                ("UserId", DatabaseSeeder.Tenant1Owner.Id.ToString()),
                ("Id", loginId.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow().AddMinutes(-10)),
                ("ModifiedAt", null),
                ("EmailConfirmationId", emailConfirmationId.ToString()),
                ("Completed", false)
            ]
        );
        Connection.Insert("EmailConfirmations", [
                ("Id", emailConfirmationId.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow().AddMinutes(-10)),
                ("ModifiedAt", null),
                ("Email", DatabaseSeeder.Tenant1Owner.Email),
                ("Type", EmailConfirmationType.Signup),
                ("OneTimePasswordHash", new PasswordHasher<object>().HashPassword(this, CorrectOneTimePassword)),
                ("ValidUntil", TimeProvider.System.GetUtcNow().AddMinutes(-10)),
                ("RetryCount", 0),
                ("ResendCount", 0),
                ("Completed", false)
            ]
        );

        var command = new CompleteLoginCommand(CorrectOneTimePassword);

        // Act
        var response = await AnonymousHttpClient.PostAsJsonAsync($"/api/account-management/authentication/login/{loginId}/complete", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, "The code is no longer valid, please request a new code.");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("EmailConfirmationExpired");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteLogin_WhenUserInviteCompleted_ShouldTrackUserInviteAcceptedEvent()
    {
        // Arrange
        // Set tenant name first (required for inviting users)
        Connection.Update("Tenants", "Id", DatabaseSeeder.Tenant1.Id.ToString(),
            [("Name", "Test Company")]
        );

        var email = Faker.Internet.Email();
        var inviteUserCommand = new InviteUserCommand(email);
        await AuthenticatedOwnerHttpClient.PostAsJsonAsync("/api/account-management/users/invite", inviteUserCommand);
        TelemetryEventsCollectorSpy.Reset();

        var (loginId, _) = await StartLogin(email);
        var command = new CompleteLoginCommand(CorrectOneTimePassword);

        // Act
        await AnonymousHttpClient.PostAsJsonAsync($"/api/account-management/authentication/login/{loginId}/complete", command);

        // Assert
        Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM Users WHERE TenantId = @tenantId AND Email = @email AND EmailConfirmed = 1",
            [new { tenantId = DatabaseSeeder.Tenant1.Id.ToString(), email = email.ToLower() }]
        ).Should().Be(1);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(3);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("LoginStarted");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("UserInviteAccepted");
        TelemetryEventsCollectorSpy.CollectedEvents[2].GetType().Name.Should().Be("LoginCompleted");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteLogin_WithValidPreferredTenant_ShouldLoginToPreferredTenant()
    {
        // Arrange
        var tenant2Id = TenantId.NewId();
        var user2Id = UserId.NewId();

        Connection.Insert("Tenants", [
                ("Id", tenant2Id.Value),
                ("CreatedAt", TimeProvider.System.GetUtcNow()),
                ("ModifiedAt", null),
                ("Name", Faker.Company.CompanyName()),
                ("State", nameof(TenantState.Active)),
                ("Logo", """{"Url":null,"Version":0}""")
            ]
        );

        Connection.Insert("Users", [
                ("TenantId", tenant2Id.Value),
                ("Id", user2Id.ToString()),
                ("CreatedAt", TimeProvider.System.GetUtcNow()),
                ("ModifiedAt", null),
                ("Email", DatabaseSeeder.Tenant1Owner.Email),
                ("EmailConfirmed", true),
                ("FirstName", Faker.Name.FirstName()),
                ("LastName", Faker.Name.LastName()),
                ("Title", null),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Role", nameof(UserRole.Owner)),
                ("Locale", "en-US")
            ]
        );

        var (loginId, _) = await StartLogin(DatabaseSeeder.Tenant1Owner.Email);
        var command = new CompleteLoginCommand(CorrectOneTimePassword, tenant2Id);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account-management/authentication/login/{loginId}/complete", command);

        // Assert
        await response.ShouldBeSuccessfulPostRequest(hasLocation: false);
        response.Headers.Count(h => h.Key == "x-refresh-token").Should().Be(1);
        response.Headers.Count(h => h.Key == "x-access-token").Should().Be(1);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("LoginCompleted");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.user_id"].Should().Be(user2Id);
    }

    [Fact]
    public async Task CompleteLogin_WithInvalidPreferredTenant_ShouldLoginToDefaultTenant()
    {
        // Arrange
        var invalidTenantId = TenantId.NewId();
        var (loginId, _) = await StartLogin(DatabaseSeeder.Tenant1Owner.Email);
        var command = new CompleteLoginCommand(CorrectOneTimePassword, invalidTenantId);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account-management/authentication/login/{loginId}/complete", command);

        // Assert
        await response.ShouldBeSuccessfulPostRequest(hasLocation: false);
        response.Headers.Count(h => h.Key == "x-refresh-token").Should().Be(1);
        response.Headers.Count(h => h.Key == "x-access-token").Should().Be(1);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("LoginCompleted");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.user_id"].Should().Be(DatabaseSeeder.Tenant1Owner.Id);
    }

    [Fact]
    public async Task CompleteLogin_WithPreferredTenantUserDoesNotHaveAccess_ShouldLoginToDefaultTenant()
    {
        // Arrange
        var tenant2Id = TenantId.NewId();

        Connection.Insert("Tenants", [
                ("Id", tenant2Id.Value),
                ("CreatedAt", TimeProvider.System.GetUtcNow()),
                ("ModifiedAt", null),
                ("Name", Faker.Company.CompanyName()),
                ("State", nameof(TenantState.Active)),
                ("Logo", """{"Url":null,"Version":0}""")
            ]
        );

        var (loginId, _) = await StartLogin(DatabaseSeeder.Tenant1Owner.Email);
        var command = new CompleteLoginCommand(CorrectOneTimePassword, tenant2Id);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account-management/authentication/login/{loginId}/complete", command);

        // Assert
        await response.ShouldBeSuccessfulPostRequest(hasLocation: false);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("LoginCompleted");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.user_id"].Should().Be(DatabaseSeeder.Tenant1Owner.Id);
    }

    private async Task<(LoginId LoginId, EmailConfirmationId emailConfirmationId)> StartLogin(string email)
    {
        var command = new StartLoginCommand(email);
        var response = await AnonymousHttpClient.PostAsJsonAsync("/api/account-management/authentication/login/start", command);
        var responseBody = await response.DeserializeResponse<StartLoginResponse>();
        return (responseBody!.LoginId, responseBody.EmailConfirmationId);
    }
}
