using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Signups.Commands;
using PlatformPlatform.AccountManagement.Signups.Domain;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using PlatformPlatform.SharedKernel.Validation;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Signups;

public sealed class SignupEndpointsTests : EndpointBaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task IsSubdomainFree_WhenTenantDoesNotExist_ShouldReturnTrue()
    {
        // Arrange
        var subdomain = Faker.Subdomain();

        // Act
        var response = await AnonymousHttpClient
            .GetAsync($"/api/account-management/signups/is-subdomain-free?subdomain={subdomain}");

        // Assert
        response.ShouldBeSuccessfulGetRequest();

        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Be("true");
    }

    [Fact]
    public async Task IsSubdomainFree_WhenTenantExists_ShouldReturnFalse()
    {
        // Arrange
        var subdomain = DatabaseSeeder.Tenant1.Id;

        // Act
        var response = await AnonymousHttpClient
            .GetAsync($"/api/account-management/signups/is-subdomain-free?subdomain={subdomain}");

        // Assert
        response.ShouldBeSuccessfulGetRequest();

        var responseBody = await response.Content.ReadAsStringAsync();
        responseBody.Should().Be("false");
    }

    [Fact]
    public async Task StartSignup_WhenSubdomainIsFreeAndEmailIsValid_ShouldReturnSuccess()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var subdomain = Faker.Subdomain();
        var command = new StartSignupCommand(subdomain, email);

        // Act
        var response = await AnonymousHttpClient.PostAsJsonAsync("/api/account-management/signups/start", command);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseBody = await response.DeserializeResponse<StartSignupResponse>();
        responseBody.Should().NotBeNull();
        responseBody!.SignupId.Should().NotBeNullOrEmpty();
        responseBody.ValidForSeconds.Should().Be(300);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].Name.Should().Be("SignupStarted");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();

        await EmailService.Received(1).SendAsync(
            email.ToLower(),
            "Confirm your email address",
            Arg.Is<string>(s => s.Contains("Your confirmation code is below")),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task StartSignup_WhenSubdomainInvalid_ShouldReturnBadRequest()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var invalidSubdomain = Faker.Random.String(31);
        var command = new StartSignupCommand(invalidSubdomain, email);

        // Act
        var response = await AnonymousHttpClient.PostAsJsonAsync("/api/account-management/signups/start", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("Subdomain", "Subdomain must be between 3 to 30 lowercase letters, numbers, or hyphens.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
        await EmailService.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), CancellationToken.None);
    }

    [Fact]
    public async Task StartSignup_WhenTenantExists_ShouldReturnBadRequest()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var subdomain = DatabaseSeeder.Tenant1.Id;
        var command = new StartSignupCommand(subdomain, email);

        // Act
        var response = await AnonymousHttpClient.PostAsJsonAsync("/api/account-management/signups/start", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("Subdomain", "The subdomain is not available.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
    }

    [Fact]
    public async Task StartSignup_WhenSignupAlreadyStarted_ShouldReturnConflict()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var subdomain = Faker.Subdomain();
        var command = new StartSignupCommand(subdomain, email);
        await AnonymousHttpClient.PostAsJsonAsync("/api/account-management/signups/start", command);

        // Act
        var response = await AnonymousHttpClient.PostAsJsonAsync("/api/account-management/signups/start", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Conflict, "Signup for this subdomain/mail has already been started. Please check your spam folder.");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1); // Only the first signup should create an event
        TelemetryEventsCollectorSpy.CollectedEvents[0].Name.Should().Be("SignupStarted");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
        await EmailService.Received(1).SendAsync(
            Arg.Is<string>(s => s.Equals(email.ToLower())),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task StartSignup_WhenTooManyAttempts_ShouldReturnTooManyRequests()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var subdomain = Faker.Subdomain();

        // Create 4 signups within the last day for this email/subdomain
        for (var i = 1; i <= 4; i++)
        {
            var oneTimePasswordHash = new PasswordHasher<object>().HashPassword(this, OneTimePasswordHelper.GenerateOneTimePassword(6));
            Connection.Insert("Signups", [
                    ("Id", SignupId.NewId().ToString()),
                    ("CreatedAt", DateTime.UtcNow.AddHours(-i)),
                    ("ModifiedAt", null),
                    ("TenantId", subdomain),
                    ("Email", email),
                    ("OneTimePasswordHash", oneTimePasswordHash),
                    ("ValidUntil", DateTime.UtcNow.AddHours(-i).AddMinutes(5)),
                    ("RetryCount", 0),
                    ("Completed", false)
                ]
            );
        }

        var command = new StartSignupCommand(subdomain, email);

        // Act
        var response = await AnonymousHttpClient.PostAsJsonAsync("/api/account-management/signups/start", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.TooManyRequests, "Too many attempts to signup with this email address. Please try again later.");

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
        await EmailService.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), CancellationToken.None);
    }

    [Fact]
    public async Task CompleteSignup_WhenValid_ShouldCreateTenantAndOwnerUser()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var subdomain = Faker.Subdomain();

        // Act
        var startSignupCommand = new StartSignupCommand(subdomain, email);
        var startSignupResponse = await AnonymousHttpClient.PostAsJsonAsync("/api/account-management/signups/start", startSignupCommand);
        startSignupResponse.EnsureSuccessStatusCode();
        var startSignupResponseBody = await startSignupResponse.DeserializeResponse<StartSignupResponse>();

        var completeStartupCommand = new CompleteSignupCommand("UNLOCK"); // UNLOCK is a special global OTP for development and tests
        var completeSignupResponse = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account-management/signups/{startSignupResponseBody!.SignupId}/complete", completeStartupCommand);

        // Assert
        await completeSignupResponse.ShouldBeSuccessfulPostRequest(hasLocation: false);
        Connection.RowExists("Tenants", subdomain).Should().BeTrue();
        Connection.ExecuteScalar("SELECT COUNT(*) FROM Users WHERE Email = @email", new { email = email.ToLower() }).Should().Be(1);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(4);
        TelemetryEventsCollectorSpy.CollectedEvents[0].Name.Should().Be("SignupStarted");
        TelemetryEventsCollectorSpy.CollectedEvents[1].Name.Should().Be("TenantCreated");
        TelemetryEventsCollectorSpy.CollectedEvents[2].Name.Should().Be("UserCreated");
        TelemetryEventsCollectorSpy.CollectedEvents[3].Name.Should().Be("SignupCompleted");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteSignup_WhenSignupNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var invalidSignupId = SignupId.NewId();
        var oneTimePassword = OneTimePasswordHelper.GenerateOneTimePassword(6);
        var command = new CompleteSignupCommand(oneTimePassword);

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account-management/signups/{invalidSignupId}/complete", command);

        // Assert
        var expectedDetail = $"Signup with id '{invalidSignupId}' not found.";
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, expectedDetail);
    }

    [Fact]
    public async Task CompleteSignup_WhenInvalidOneTimePassword_ShouldReturnBadRequest()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var subdomain = Faker.Subdomain();

        var signupId = SignupId.NewId().ToString();
        Connection.Insert("Signups", [
                ("Id", signupId),
                ("CreatedAt", DateTime.UtcNow.AddMinutes(-10)),
                ("ModifiedAt", null),
                ("TenantId", subdomain),
                ("Email", email),
                ("OneTimePasswordHash", new PasswordHasher<object>().HashPassword(this, OneTimePasswordHelper.GenerateOneTimePassword(6))),
                ("ValidUntil", DateTime.UtcNow.AddMinutes(-5)),
                ("RetryCount", 0),
                ("Completed", false)
            ]
        );

        const string oneTimePassword = "FAULTY";
        var command = new CompleteSignupCommand(oneTimePassword);

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account-management/signups/{signupId}/complete", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, "The code is wrong or no longer valid.");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].Name.Should().Be("SignupFailed");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteSignup_WhenSignupAlreadyCompleted_ShouldReturnBadRequest()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var subdomain = Faker.Subdomain();

        var oneTimePassword = OneTimePasswordHelper.GenerateOneTimePassword(6);
        var signupId = SignupId.NewId().ToString();
        Connection.Insert("Signups", [
                ("Id", signupId),
                ("CreatedAt", DateTime.UtcNow.AddMinutes(-2)),
                ("ModifiedAt", DateTime.UtcNow.AddMinutes(-1)),
                ("TenantId", subdomain),
                ("Email", email),
                ("OneTimePasswordHash", new PasswordHasher<object>().HashPassword(this, oneTimePassword)),
                ("ValidUntil", DateTime.UtcNow.AddMinutes(3)),
                ("RetryCount", 0),
                ("Completed", true)
            ]
        );

        var command = new CompleteSignupCommand(oneTimePassword);

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account-management/signups/{signupId}/complete", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, $"The signup with id {signupId} for tenant {subdomain} has already been completed.");

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();
    }

    [Fact]
    public async Task CompleteSignup_WhenRetryCountExceeded_ShouldReturnForbidden()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var subdomain = Faker.Subdomain();

        var oneTimePassword = OneTimePasswordHelper.GenerateOneTimePassword(6);
        var signupId = SignupId.NewId().ToString();
        Connection.Insert("Signups", [
                ("Id", signupId),
                ("CreatedAt", DateTime.UtcNow.AddMinutes(-2)),
                ("ModifiedAt", DateTime.UtcNow.AddMinutes(-1)),
                ("TenantId", subdomain),
                ("Email", email),
                ("OneTimePasswordHash", new PasswordHasher<object>().HashPassword(this, oneTimePassword)),
                ("ValidUntil", DateTime.UtcNow.AddMinutes(3)),
                ("RetryCount", 3),
                ("Completed", false)
            ]
        );

        var command = new CompleteSignupCommand(oneTimePassword);

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account-management/signups/{signupId}/complete", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "To many attempts, please request a new code.");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].Name.Should().Be("SignupBlocked");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteSignup_WhenSignupExpired_ShouldReturnBadRequest()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var subdomain = Faker.Subdomain();

        var oneTimePassword = OneTimePasswordHelper.GenerateOneTimePassword(6);
        var signupId = SignupId.NewId().ToString();
        Connection.Insert("Signups", [
                ("Id", signupId),
                ("CreatedAt", DateTime.UtcNow.AddMinutes(-10)),
                ("ModifiedAt", null),
                ("TenantId", subdomain),
                ("Email", email),
                ("OneTimePasswordHash", new PasswordHasher<object>().HashPassword(this, oneTimePassword)),
                ("ValidUntil", DateTime.UtcNow.AddMinutes(-5)),
                ("RetryCount", 0),
                ("Completed", false)
            ]
        );

        var command = new CompleteSignupCommand(oneTimePassword);

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account-management/signups/{signupId}/complete", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, "The code is no longer valid, please request a new code.");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].Name.Should().Be("SignupExpired");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }
}
