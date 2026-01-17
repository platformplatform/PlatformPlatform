using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.EmailAuthentication.Domain;
using PlatformPlatform.AccountManagement.Features.Signups.Commands;
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
        var emailConfirmationId = await StartSignup(email);

        var command = new CompleteEmailSignupCommand(CorrectOneTimePassword, "en-US");

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account-management/authentication/email/signup/{emailConfirmationId}/complete", command);

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
        var invalidEmailConfirmationId = EmailConfirmationId.NewId();
        var command = new CompleteEmailSignupCommand(CorrectOneTimePassword, "en-US");

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account-management/authentication/email/signup/{invalidEmailConfirmationId}/complete", command);

        // Assert
        var expectedDetail = $"Email confirmation with id '{invalidEmailConfirmationId}' not found.";
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, expectedDetail);
    }

    [Fact]
    public async Task CompleteSignup_WhenInvalidOneTimePassword_ShouldReturnBadRequest()
    {
        // Arrange
        var emailConfirmationId = await StartSignup(Faker.Internet.UniqueEmail());

        var command = new CompleteEmailSignupCommand(WrongOneTimePassword, "en-US");

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account-management/authentication/email/signup/{emailConfirmationId}/complete", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, "The code is wrong or no longer valid.");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(2);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("SignupStarted");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("EmailConfirmationFailed");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteSignup_WhenSignupAlreadyCompleted_ShouldReturnBadRequest()
    {
        // Arrange
        var emailConfirmationId = await StartSignup(Faker.Internet.UniqueEmail());

        var command = new CompleteEmailSignupCommand(CorrectOneTimePassword, "en-US") { EmailConfirmationId = emailConfirmationId };
        await AnonymousHttpClient.PostAsJsonAsync($"/api/account-management/authentication/email/signup/{emailConfirmationId}/complete", command);

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account-management/authentication/email/signup/{emailConfirmationId}/complete", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, $"Email confirmation with id {emailConfirmationId} has already been completed.");
    }

    [Fact]
    public async Task CompleteSignup_WhenRetryCountExceeded_ShouldReturnForbidden()
    {
        // Arrange
        var emailConfirmationId = await StartSignup(Faker.Internet.UniqueEmail());

        var command = new CompleteEmailSignupCommand(WrongOneTimePassword, "en-US");
        await AnonymousHttpClient.PostAsJsonAsync($"/api/account-management/authentication/email/signup/{emailConfirmationId}/complete", command);
        await AnonymousHttpClient.PostAsJsonAsync($"/api/account-management/authentication/email/signup/{emailConfirmationId}/complete", command);
        await AnonymousHttpClient.PostAsJsonAsync($"/api/account-management/authentication/email/signup/{emailConfirmationId}/complete", command);

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account-management/authentication/email/signup/{emailConfirmationId}/complete", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Too many attempts, please request a new code.");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(5);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("SignupStarted");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("EmailConfirmationFailed");
        TelemetryEventsCollectorSpy.CollectedEvents[2].GetType().Name.Should().Be("EmailConfirmationFailed");
        TelemetryEventsCollectorSpy.CollectedEvents[3].GetType().Name.Should().Be("EmailConfirmationFailed");
        TelemetryEventsCollectorSpy.CollectedEvents[4].GetType().Name.Should().Be("EmailConfirmationBlocked");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteSignup_WhenSignupExpired_ShouldReturnBadRequest()
    {
        // Arrange
        var email = Faker.Internet.UniqueEmail();

        var emailConfirmationId = EmailConfirmationId.NewId();
        Connection.Insert("EmailConfirmations", [
                ("Id", emailConfirmationId.ToString()),
                ("CreatedAt", TimeProvider.GetUtcNow().AddMinutes(-10)),
                ("ModifiedAt", null),
                ("Email", email),
                ("Type", EmailConfirmationType.Signup),
                ("OneTimePasswordHash", new PasswordHasher<object>().HashPassword(this, CorrectOneTimePassword)),
                ("RetryCount", 0),
                ("ResendCount", 0),
                ("Completed", false)
            ]
        );

        var command = new CompleteEmailSignupCommand(CorrectOneTimePassword, "en-US") { EmailConfirmationId = emailConfirmationId };

        // Act
        var response = await AnonymousHttpClient
            .PostAsJsonAsync($"/api/account-management/authentication/email/signup/{emailConfirmationId}/complete", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, "The code is no longer valid, please request a new code.");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("EmailConfirmationExpired");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    private async Task<EmailConfirmationId> StartSignup(string email)
    {
        var command = new StartEmailSignupCommand(email);
        var response = await AnonymousHttpClient.PostAsJsonAsync("/api/account-management/authentication/email/signup/start", command);
        var responseBody = await response.DeserializeResponse<StartEmailSignupResponse>();
        return responseBody!.EmailConfirmationId;
    }
}
