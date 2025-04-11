using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.EmailConfirmations.Domain;
using PlatformPlatform.AccountManagement.Features.Signups.Commands;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using PlatformPlatform.SharedKernel.Validation;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Signups;

public sealed class StartSignupTests : EndpointBaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task StartSignup_WhenEmailIsValid_ShouldReturnSuccess()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var command = new StartSignupCommand(email);

        // Act
        var response = await AnonymousHttpClient.PostAsJsonAsync("/api/account-management/signups/start", command);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseBody = await response.DeserializeResponse<StartSignupResponse>();
        responseBody.Should().NotBeNull();
        responseBody.EmailConfirmationId.ToString().Should().NotBeNullOrEmpty();
        responseBody.ValidForSeconds.Should().Be(300);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("SignupStarted");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();

        await EmailClient.Received(1).SendAsync(
            email.ToLower(),
            "Confirm your email address",
            Arg.Is<string>(s => s.Contains("Your confirmation code is below")),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task StartSignup_WhenInvalidEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var invalidEmail = "invalid email";
        var command = new StartSignupCommand(invalidEmail);

        // Act
        var response = await AnonymousHttpClient.PostAsJsonAsync("/api/account-management/signups/start", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("Email", "Email must be in a valid format and no longer than 100 characters.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
        await EmailClient.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), CancellationToken.None);
    }

    [Fact]
    public async Task StartSignup_WhenSignupAlreadyStarted_ShouldReturnConflict()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var command = new StartSignupCommand(email);
        await AnonymousHttpClient.PostAsJsonAsync("/api/account-management/signups/start", command);

        // Act
        var response = await AnonymousHttpClient.PostAsJsonAsync("/api/account-management/signups/start", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Conflict, "Email confirmation for this email has already been started. Please check your spam folder.");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1); // Only the first signup should create an event
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("SignupStarted");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
        await EmailClient.Received(1).SendAsync(
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
        var email = Faker.Internet.Email().ToLowerInvariant();

        // Create 4 signups within the last day for this email
        for (var i = 1; i <= 4; i++)
        {
            var oneTimePasswordHash = new PasswordHasher<object>().HashPassword(this, OneTimePasswordHelper.GenerateOneTimePassword(6));
            Connection.Insert("EmailConfirmations", [
                    ("Id", EmailConfirmationId.NewId().ToString()),
                    ("CreatedAt", TimeProvider.System.GetUtcNow().AddHours(-i)),
                    ("ModifiedAt", null),
                    ("Email", email),
                    ("Type", EmailConfirmationType.Signup.ToString()),
                    ("OneTimePasswordHash", oneTimePasswordHash),
                    ("ValidUntil", TimeProvider.System.GetUtcNow().AddHours(-i).AddMinutes(5)),
                    ("RetryCount", 0),
                    ("ResendCount", 0),
                    ("Completed", false)
                ]
            );
        }

        var command = new StartSignupCommand(email);

        // Act
        var response = await AnonymousHttpClient.PostAsJsonAsync("/api/account-management/signups/start", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.TooManyRequests, "Too many attempts to confirm this email address. Please try again later.");

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
        await EmailClient.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), CancellationToken.None);
    }
}
