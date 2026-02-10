using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.EmailAuthentication.Commands;
using PlatformPlatform.AccountManagement.Features.EmailAuthentication.Domain;
using PlatformPlatform.AccountManagement.Features.Tenants.EventHandlers;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Signups;

public sealed class CompleteEmailSignupTests : EndpointBaseTest<AccountManagementDbContext>
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
            .PostAsJsonAsync($"/api/account-management/authentication/email/signup/{emailLoginId}/complete", command);

        // Assert
        await response.ShouldBeSuccessfulPostRequest(hasLocation: false);
        Connection.ExecuteScalar<long>("SELECT COUNT(*) FROM Users WHERE Email = @email", [new { email = email.ToLower() }]).Should().Be(1);

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
            .PostAsJsonAsync($"/api/account-management/authentication/email/signup/{invalidEmailLoginId}/complete", command);

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
            .PostAsJsonAsync($"/api/account-management/authentication/email/signup/{emailLoginId}/complete", command);

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
        await AnonymousHttpClient.PostAsJsonAsync($"/api/account-management/authentication/email/signup/{emailLoginId}/complete", command);

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account-management/authentication/email/signup/{emailLoginId}/complete", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, $"Email login with id '{emailLoginId}' has already been completed.");
    }

    [Fact]
    public async Task CompleteSignup_WhenRetryCountExceeded_ShouldReturnForbidden()
    {
        // Arrange
        var emailLoginId = await StartSignup(Faker.Internet.UniqueEmail());

        var command = new CompleteEmailSignupCommand(WrongOneTimePassword, "en-US");
        await AnonymousHttpClient.PostAsJsonAsync($"/api/account-management/authentication/email/signup/{emailLoginId}/complete", command);
        await AnonymousHttpClient.PostAsJsonAsync($"/api/account-management/authentication/email/signup/{emailLoginId}/complete", command);
        await AnonymousHttpClient.PostAsJsonAsync($"/api/account-management/authentication/email/signup/{emailLoginId}/complete", command);

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account-management/authentication/email/signup/{emailLoginId}/complete", command);

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
        Connection.Insert("EmailLogins", [
                ("Id", emailLoginId.ToString()),
                ("CreatedAt", TimeProvider.GetUtcNow().AddMinutes(-10)),
                ("ModifiedAt", null),
                ("Email", email),
                ("Type", nameof(EmailLoginType.Signup)),
                ("OneTimePasswordHash", new PasswordHasher<object>().HashPassword(this, CorrectOneTimePassword)),
                ("RetryCount", 0),
                ("ResendCount", 0),
                ("Completed", false)
            ]
        );

        var command = new CompleteEmailSignupCommand(CorrectOneTimePassword, "en-US") { EmailLoginId = emailLoginId };

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account-management/authentication/email/signup/{emailLoginId}/complete", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, "The code is no longer valid, please request a new code.");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("EmailLoginCodeExpired");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    private async Task<EmailLoginId> StartSignup(string email)
    {
        var command = new StartEmailSignupCommand(email);
        var response = await AnonymousHttpClient.PostAsJsonAsync("/api/account-management/authentication/email/signup/start", command);
        var responseBody = await response.DeserializeResponse<StartEmailSignupResponse>();
        return responseBody!.EmailLoginId;
    }
}
