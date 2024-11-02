using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.Authentication.Commands;
using PlatformPlatform.AccountManagement.Features.Authentication.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Commands;
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
        var loginId = await StartLogin(DatabaseSeeder.User1.Email);
        var command = new CompleteLoginCommand(CorrectOneTimePassword);

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account-management/authentication/login/{loginId}/complete", command);

        // Assert
        await response.ShouldBeSuccessfulPostRequest(hasLocation: false);
        var updatedLoginCount = Connection.ExecuteScalar("SELECT COUNT(*) FROM Logins WHERE Id = @id AND Completed = 1", new { id = loginId });
        updatedLoginCount.Should().Be(1);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(2);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("LoginStarted");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("LoginCompleted");
        TelemetryEventsCollectorSpy.CollectedEvents[1].Properties["event.user_id"].Should().Be(DatabaseSeeder.User1.Id);
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();

        response.Headers.Count(h => h.Key == "x-refresh-token").Should().Be(1);
        response.Headers.Count(h => h.Key == "x-access-token").Should().Be(1);
    }

    [Fact]
    public async Task CompleteLogin_WhenLoginNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var invalidLoginId = LoginId.NewId();
        var command = new CompleteLoginCommand(CorrectOneTimePassword);

        // Act
        var response = await AnonymousHttpClient.PostAsJsonAsync($"/api/account-management/authentication/login/{invalidLoginId}/complete", command);

        // Assert
        var expectedDetail = $"Login with id '{invalidLoginId}' not found.";
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, expectedDetail);

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task CompleteLogin_WhenInvalidOneTimePassword_ShouldReturnBadRequest()
    {
        // Arrange
        var loginId = await StartLogin(DatabaseSeeder.User1.Email);
        var command = new CompleteLoginCommand(WrongOneTimePassword);

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account-management/authentication/login/{loginId}/complete", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, "The code is wrong or no longer valid.");

        // Verify retry count increment and event collection
        var loginCompleted = Connection.ExecuteScalar("SELECT Completed FROM Logins WHERE Id = @id", new { id = loginId });
        loginCompleted.Should().Be(0);
        var updatedRetryCount = Connection.ExecuteScalar("SELECT RetryCount FROM Logins WHERE Id = @id", new { id = loginId });
        updatedRetryCount.Should().Be(1);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(2);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("LoginStarted");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("LoginFailed");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteLogin_WhenLoginAlreadyCompleted_ShouldReturnBadRequest()
    {
        // Arrange
        var loginId = await StartLogin(DatabaseSeeder.User1.Email);
        var command = new CompleteLoginCommand(CorrectOneTimePassword);
        await AnonymousHttpClient.PostAsJsonAsync($"/api/account-management/authentication/login/{loginId}/complete", command);

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account-management/authentication/login/{loginId}/complete", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(
            HttpStatusCode.BadRequest, $"The login process {loginId} for user {DatabaseSeeder.User1.Id} has already been completed."
        );
    }

    [Fact]
    public async Task CompleteLogin_WhenRetryCountExceeded_ShouldReturnForbidden()
    {
        // Arrange
        var loginId = await StartLogin(DatabaseSeeder.User1.Email);
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
        var loginCompleted = Connection.ExecuteScalar("SELECT Completed FROM Logins WHERE Id = @id", new { id = loginId });
        loginCompleted.Should().Be(0);
        var updatedRetryCount = Connection.ExecuteScalar("SELECT RetryCount FROM Logins WHERE Id = @id", new { id = loginId });
        updatedRetryCount.Should().Be(4);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(5);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("LoginStarted");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("LoginFailed");
        TelemetryEventsCollectorSpy.CollectedEvents[2].GetType().Name.Should().Be("LoginFailed");
        TelemetryEventsCollectorSpy.CollectedEvents[3].GetType().Name.Should().Be("LoginFailed");
        TelemetryEventsCollectorSpy.CollectedEvents[4].GetType().Name.Should().Be("LoginBlocked");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteLogin_WhenLoginExpired_ShouldReturnBadRequest()
    {
        // Arrange
        var loginId = LoginId.NewId();
        var oneTimePasswordHash = new PasswordHasher<object>().HashPassword(this, CorrectOneTimePassword);

        // Insert expired login
        Connection.Insert("Logins", [
                ("TenantId", DatabaseSeeder.User1.TenantId.ToString()),
                ("UserId", DatabaseSeeder.User1.Id.ToString()),
                ("Id", loginId.ToString()),
                ("CreatedAt", DateTime.UtcNow.AddMinutes(-10)),
                ("ModifiedAt", null),
                ("OneTimePasswordHash", oneTimePasswordHash),
                ("ValidUntil", DateTime.UtcNow.AddMinutes(-5)),
                ("RetryCount", 0),
                ("Completed", false)
            ]
        );

        var command = new CompleteLoginCommand(CorrectOneTimePassword);

        // Act
        var response = await AnonymousHttpClient.PostAsJsonAsync($"/api/account-management/authentication/login/{loginId}/complete", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, "The code is no longer valid, please request a new code.");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("LoginExpired");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteLogin_WhenUserInviteCompleted_ShouldTrackUserInviteAcceptedEvent()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var inviteUserCommand = new InviteUserCommand(email);
        await AuthenticatedHttpClient.PostAsJsonAsync("/api/account-management/users/invite", inviteUserCommand);
        TelemetryEventsCollectorSpy.Reset();

        var loginId = await StartLogin(email);
        var command = new CompleteLoginCommand(CorrectOneTimePassword);

        // Act
        await AnonymousHttpClient.PostAsJsonAsync($"/api/account-management/authentication/login/{loginId}/complete", command);

        // Assert
        Connection.ExecuteScalar(
            "SELECT COUNT(*) FROM Users WHERE TenantId = @tenantId AND Email = @email AND EmailConfirmed = 1",
            new { tenantId = DatabaseSeeder.Tenant1.Id.ToString(), email = email.ToLower() }
        ).Should().Be(1);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(3);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("LoginStarted");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("UserInviteAccepted");
        TelemetryEventsCollectorSpy.CollectedEvents[2].GetType().Name.Should().Be("LoginCompleted");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    private async Task<string> StartLogin(string email)
    {
        var command = new StartLoginCommand(email);
        var response = await AnonymousHttpClient.PostAsJsonAsync("/api/account-management/authentication/login/start", command);
        var responseBody = await response.DeserializeResponse<StartLoginResponse>();
        return responseBody!.LoginId;
    }
}
