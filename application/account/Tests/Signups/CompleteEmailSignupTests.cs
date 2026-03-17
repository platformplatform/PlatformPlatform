using System.Net;
using System.Net.Http.Json;
using Account.Database;
using Account.Features.EmailAuthentication.Commands;
using Account.Features.EmailAuthentication.Domain;
using Account.Features.Tenants.EventHandlers;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using SharedKernel.Tests;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.Signups;

public sealed class CompleteEmailSignupTests : EndpointBaseTest<AccountDbContext>
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
        var email = Faker.Internet.UniqueEmail();
        var emailLoginId = await StartSignup(email);

        var command = new CompleteEmailSignupCommand(CorrectOneTimePassword, "en-US");

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account/authentication/email/signup/{emailLoginId}/complete", command);

        // Assert
        await response.ShouldBeSuccessfulPostRequest(hasLocation: false);
        Connection.ExecuteScalar<long>("SELECT COUNT(*) FROM users WHERE email = @email", [new { email = email.ToLower() }]).Should().Be(1);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(5);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("SignupStarted");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("TenantCreated");
        TelemetryEventsCollectorSpy.CollectedEvents[2].GetType().Name.Should().Be("UserCreated");
        TelemetryEventsCollectorSpy.CollectedEvents[3].GetType().Name.Should().Be("SessionCreated");
        TelemetryEventsCollectorSpy.CollectedEvents[4].GetType().Name.Should().Be("SignupCompleted");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();

        _tenantCreatedEventHandlerLogger.Received().LogInformation("Raise event to send Welcome mail to tenant");
    }

    [Fact]
    public async Task CompleteSignup_WhenSignupNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var invalidEmailLoginId = EmailLoginId.NewId();
        var command = new CompleteEmailSignupCommand(CorrectOneTimePassword, "en-US");

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account/authentication/email/signup/{invalidEmailLoginId}/complete", command);

        // Assert
        var expectedDetail = $"Email login with id '{invalidEmailLoginId}' not found.";
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, expectedDetail);
    }

    [Fact]
    public async Task CompleteSignup_WhenInvalidOneTimePassword_ShouldReturnBadRequest()
    {
        // Arrange
        var emailLoginId = await StartSignup(Faker.Internet.UniqueEmail());

        var command = new CompleteEmailSignupCommand(WrongOneTimePassword, "en-US");

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account/authentication/email/signup/{emailLoginId}/complete", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, "The code is wrong or no longer valid.");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(2);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("SignupStarted");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("EmailLoginCodeFailed");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteSignup_WhenSignupAlreadyCompleted_ShouldReturnBadRequest()
    {
        // Arrange
        var emailLoginId = await StartSignup(Faker.Internet.UniqueEmail());

        var command = new CompleteEmailSignupCommand(CorrectOneTimePassword, "en-US") { EmailLoginId = emailLoginId };
        await AnonymousHttpClient.PostAsJsonAsync($"/api/account/authentication/email/signup/{emailLoginId}/complete", command);

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account/authentication/email/signup/{emailLoginId}/complete", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, $"Email login with id '{emailLoginId}' has already been completed.");
    }

    [Fact]
    public async Task CompleteSignup_WhenRetryCountExceeded_ShouldReturnForbidden()
    {
        // Arrange
        var emailLoginId = await StartSignup(Faker.Internet.UniqueEmail());

        var command = new CompleteEmailSignupCommand(WrongOneTimePassword, "en-US");
        await AnonymousHttpClient.PostAsJsonAsync($"/api/account/authentication/email/signup/{emailLoginId}/complete", command);
        await AnonymousHttpClient.PostAsJsonAsync($"/api/account/authentication/email/signup/{emailLoginId}/complete", command);
        await AnonymousHttpClient.PostAsJsonAsync($"/api/account/authentication/email/signup/{emailLoginId}/complete", command);

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account/authentication/email/signup/{emailLoginId}/complete", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Too many attempts, please request a new code.");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(5);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("SignupStarted");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("EmailLoginCodeFailed");
        TelemetryEventsCollectorSpy.CollectedEvents[2].GetType().Name.Should().Be("EmailLoginCodeFailed");
        TelemetryEventsCollectorSpy.CollectedEvents[3].GetType().Name.Should().Be("EmailLoginCodeFailed");
        TelemetryEventsCollectorSpy.CollectedEvents[4].GetType().Name.Should().Be("EmailLoginCodeBlocked");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteSignup_WhenSignupExpired_ShouldReturnBadRequest()
    {
        // Arrange
        var email = Faker.Internet.UniqueEmail();

        var emailLoginId = EmailLoginId.NewId();
        Connection.Insert("email_logins", [
                ("id", emailLoginId.ToString()),
                ("created_at", TimeProvider.GetUtcNow().AddMinutes(-10)),
                ("modified_at", null),
                ("email", email),
                ("type", nameof(EmailLoginType.Signup)),
                ("one_time_password_hash", new PasswordHasher<object>().HashPassword(this, CorrectOneTimePassword)),
                ("retry_count", 0),
                ("resend_count", 0),
                ("completed", false)
            ]
        );

        var command = new CompleteEmailSignupCommand(CorrectOneTimePassword, "en-US") { EmailLoginId = emailLoginId };

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account/authentication/email/signup/{emailLoginId}/complete", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, "The code is no longer valid, please request a new code.");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("EmailLoginCodeExpired");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    private async Task<EmailLoginId> StartSignup(string email)
    {
        var command = new StartEmailSignupCommand(email);
        var response = await AnonymousHttpClient.PostAsJsonAsync("/api/account/authentication/email/signup/start", command);
        var responseBody = await response.DeserializeResponse<StartEmailSignupResponse>();
        return responseBody!.EmailLoginId;
    }
}
