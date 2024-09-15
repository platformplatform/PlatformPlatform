using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Signups.Commands;
using PlatformPlatform.AccountManagement.Signups.Domain;
using PlatformPlatform.AccountManagement.Tenants.EventHandlers;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Signups;

public sealed class CompleteSignupTests : EndpointBaseTest<AccountManagementDbContext>
{
    private const string CorrectOneTimePassword = "UNLOCK"; // UNLOCK is a special global OTP for development and tests
    private const string WrongOneTimePassword = "FAULTY";
    private readonly ILogger<TenantCreatedEventHandler> _tenantCreatedEventHandlerLogger = Substitute.For<ILogger<TenantCreatedEventHandler>>();

    protected override void RegisterMockLoggers(IServiceCollection services)
    {
        services.AddSingleton(_tenantCreatedEventHandlerLogger);
    }

    [Fact]
    public async Task CompleteSignup_WhenValid_ShouldCreateTenantAndOwnerUser()
    {
        // Arrange
        var subdomain = Faker.Subdomain();
        var email = Faker.Internet.Email();
        var signupId = await StartSignup(subdomain, email);

        var command = new CompleteSignupCommand(CorrectOneTimePassword);

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account-management/signups/{signupId}/complete", command);

        // Assert
        await response.ShouldBeSuccessfulPostRequest(hasLocation: false);
        Connection.RowExists("Tenants", subdomain).Should().BeTrue();
        Connection.ExecuteScalar("SELECT COUNT(*) FROM Users WHERE Email = @email", new { email = email.ToLower() }).Should().Be(1);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(4);
        TelemetryEventsCollectorSpy.CollectedEvents[0].Name.Should().Be("SignupStarted");
        TelemetryEventsCollectorSpy.CollectedEvents[1].Name.Should().Be("TenantCreated");
        TelemetryEventsCollectorSpy.CollectedEvents[2].Name.Should().Be("UserCreated");
        TelemetryEventsCollectorSpy.CollectedEvents[3].Name.Should().Be("SignupCompleted");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();

        _tenantCreatedEventHandlerLogger.Received().LogInformation("Raise event to send Welcome mail to tenant.");
    }

    [Fact]
    public async Task CompleteSignup_WhenSignupNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var invalidSignupId = SignupId.NewId();
        var command = new CompleteSignupCommand(CorrectOneTimePassword);

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
        var signupId = await StartSignup(Faker.Subdomain(), Faker.Internet.Email());

        var command = new CompleteSignupCommand(WrongOneTimePassword);

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account-management/signups/{signupId}/complete", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, "The code is wrong or no longer valid.");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(2);
        TelemetryEventsCollectorSpy.CollectedEvents[0].Name.Should().Be("SignupStarted");
        TelemetryEventsCollectorSpy.CollectedEvents[1].Name.Should().Be("SignupFailed");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteSignup_WhenSignupAlreadyCompleted_ShouldReturnBadRequest()
    {
        // Arrange
        var subdomain = Faker.Subdomain();
        var signupId = await StartSignup(subdomain, Faker.Internet.Email());

        var command = new CompleteSignupCommand(CorrectOneTimePassword);
        await AnonymousHttpClient.PostAsJsonAsync($"/api/account-management/signups/{signupId}/complete", command);

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account-management/signups/{signupId}/complete", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, $"The signup with id {signupId} for tenant {subdomain} has already been completed.");
    }

    [Fact]
    public async Task CompleteSignup_WhenRetryCountExceeded_ShouldReturnForbidden()
    {
        // Arrange
        var signupId = await StartSignup(Faker.Subdomain(), Faker.Internet.Email());

        var command = new CompleteSignupCommand(WrongOneTimePassword);
        await AnonymousHttpClient.PostAsJsonAsync($"/api/account-management/signups/{signupId}/complete", command);
        await AnonymousHttpClient.PostAsJsonAsync($"/api/account-management/signups/{signupId}/complete", command);
        await AnonymousHttpClient.PostAsJsonAsync($"/api/account-management/signups/{signupId}/complete", command);

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account-management/signups/{signupId}/complete", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "To many attempts, please request a new code.");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(5);
        TelemetryEventsCollectorSpy.CollectedEvents[0].Name.Should().Be("SignupStarted");
        TelemetryEventsCollectorSpy.CollectedEvents[1].Name.Should().Be("SignupFailed");
        TelemetryEventsCollectorSpy.CollectedEvents[2].Name.Should().Be("SignupFailed");
        TelemetryEventsCollectorSpy.CollectedEvents[3].Name.Should().Be("SignupFailed");
        TelemetryEventsCollectorSpy.CollectedEvents[4].Name.Should().Be("SignupBlocked");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteSignup_WhenSignupExpired_ShouldReturnBadRequest()
    {
        // Arrange
        var subdomain = Faker.Subdomain();
        var email = Faker.Internet.Email();

        var signupId = SignupId.NewId().ToString();
        Connection.Insert("Signups", [
                ("Id", signupId),
                ("CreatedAt", DateTime.UtcNow.AddMinutes(-10)),
                ("ModifiedAt", null),
                ("TenantId", subdomain),
                ("Email", email),
                ("OneTimePasswordHash", new PasswordHasher<object>().HashPassword(this, CorrectOneTimePassword)),
                ("ValidUntil", DateTime.UtcNow.AddMinutes(-5)),
                ("RetryCount", 0),
                ("Completed", false)
            ]
        );

        var command = new CompleteSignupCommand(CorrectOneTimePassword);

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account-management/signups/{signupId}/complete", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, "The code is no longer valid, please request a new code.");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].Name.Should().Be("SignupExpired");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    private async Task<string> StartSignup(string subdomain, string email)
    {
        var command = new StartSignupCommand(subdomain, email);
        var response = await AnonymousHttpClient.PostAsJsonAsync("/api/account-management/signups/start", command);
        var responseBody = await response.DeserializeResponse<StartSignupResponse>();
        return responseBody!.SignupId;
    }
}
