using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using PlatformPlatform.Account.Database;
using PlatformPlatform.Account.Features.EmailAuthentication.Commands;
using PlatformPlatform.Account.Features.EmailAuthentication.Domain;
using PlatformPlatform.Account.Features.Tenants.Domain;
using PlatformPlatform.Account.Features.Users.Commands;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using Xunit;

namespace PlatformPlatform.Account.Tests.EmailAuthentication;

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
            "SELECT COUNT(*) FROM EmailLogins WHERE Id = @id AND Completed = 1", [new { id = emailLoginId.ToString() }]
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

        var updatedRetryCount = Connection.ExecuteScalar<long>("SELECT RetryCount FROM EmailLogins WHERE Id = @id", [new { id = emailLoginId.ToString() }]);
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
            "SELECT RetryCount FROM EmailLogins WHERE Id = @id", [new { id = emailLoginId.ToString() }]
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

        Connection.Insert("EmailLogins", [
                ("Id", emailLoginId.ToString()),
                ("CreatedAt", TimeProvider.GetUtcNow().AddMinutes(-10)),
                ("ModifiedAt", null),
                ("Email", DatabaseSeeder.Tenant1Owner.Email),
                ("Type", nameof(EmailLoginType.Login)),
                ("OneTimePasswordHash", new PasswordHasher<object>().HashPassword(this, CorrectOneTimePassword)),
                ("RetryCount", 0),
                ("ResendCount", 0),
                ("Completed", false)
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
        Connection.Update("Tenants", "Id", DatabaseSeeder.Tenant1.Id.ToString(), [("Name", "Test Company")]);

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
            "SELECT COUNT(*) FROM Users WHERE TenantId = @tenantId AND Email = @email AND EmailConfirmed = 1",
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

        Connection.Insert("Tenants", [
                ("Id", tenant2Id.Value),
                ("CreatedAt", TimeProvider.GetUtcNow()),
                ("ModifiedAt", null),
                ("Name", Faker.Company.CompanyName()),
                ("State", nameof(TenantState.Active)),
                ("Logo", """{"Url":null,"Version":0}""")
            ]
        );

        Connection.Insert("Users", [
                ("TenantId", tenant2Id.Value),
                ("Id", user2Id.ToString()),
                ("CreatedAt", TimeProvider.GetUtcNow()),
                ("ModifiedAt", null),
                ("Email", DatabaseSeeder.Tenant1Owner.Email),
                ("EmailConfirmed", true),
                ("FirstName", Faker.Name.FirstName()),
                ("LastName", Faker.Name.LastName()),
                ("Title", null),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Role", nameof(UserRole.Owner)),
                ("Locale", "en-US"),
                ("ExternalIdentities", "[]")
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

        Connection.Insert("Tenants", [
                ("Id", tenant2Id.Value),
                ("CreatedAt", TimeProvider.GetUtcNow()),
                ("ModifiedAt", null),
                ("Name", Faker.Company.CompanyName()),
                ("State", nameof(TenantState.Active)),
                ("Logo", """{"Url":null,"Version":0}""")
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
