using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using PlatformPlatform.Account.Database;
using PlatformPlatform.Account.Features.EmailAuthentication.Commands;
using PlatformPlatform.Account.Features.EmailAuthentication.Domain;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using PlatformPlatform.SharedKernel.Validation;
using Xunit;

namespace PlatformPlatform.Account.Tests.Signups;

public sealed class StartEmailSignupTests : EndpointBaseTest<AccountDbContext>
{
    [Fact]
    public async Task StartSignup_WhenEmailIsValid_ShouldReturnSuccess()
    {
        // Arrange
        var email = Faker.Internet.UniqueEmail();
        var command = new StartEmailSignupCommand(email);

        // Act
        var response = await AnonymousHttpClient.PostAsJsonAsync("/api/account/authentication/email/signup/start", command);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseBody = await response.DeserializeResponse<StartEmailSignupResponse>();
        responseBody.Should().NotBeNull();
        responseBody.EmailLoginId.ToString().Should().NotBeNullOrEmpty();
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
        var command = new StartEmailSignupCommand(invalidEmail);

        // Act
        var response = await AnonymousHttpClient.PostAsJsonAsync("/api/account/authentication/email/signup/start", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("email", "Email must be in a valid format and no longer than 100 characters.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
        await EmailClient.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), CancellationToken.None);
    }

    [Fact]
    public async Task StartSignup_WhenTooManyAttempts_ShouldReturnTooManyRequests()
    {
        // Arrange
        var email = Faker.Internet.UniqueEmail().ToLowerInvariant();

        // Create 4 signups within the last hour for this email
        for (var i = 1; i <= 4; i++)
        {
            var oneTimePasswordHash = new PasswordHasher<object>().HashPassword(this, OneTimePasswordHelper.GenerateOneTimePassword(6));
            Connection.Insert("EmailLogins", [
                    ("Id", EmailLoginId.NewId().ToString()),
                    ("CreatedAt", TimeProvider.GetUtcNow().AddMinutes(-10)),
                    ("ModifiedAt", null),
                    ("Email", email),
                    ("Type", nameof(EmailLoginType.Signup)),
                    ("OneTimePasswordHash", oneTimePasswordHash),
                    ("RetryCount", 0),
                    ("ResendCount", 0),
                    ("Completed", false)
                ]
            );
        }

        var command = new StartEmailSignupCommand(email);

        // Act
        var response = await AnonymousHttpClient.PostAsJsonAsync("/api/account/authentication/email/signup/start", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.TooManyRequests, "Too many attempts to confirm this email address. Please try again later.");

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
        await EmailClient.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), CancellationToken.None);
    }
}
