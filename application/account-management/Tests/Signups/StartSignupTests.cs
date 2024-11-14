using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.Signups.Commands;
using PlatformPlatform.AccountManagement.Features.Signups.Domain;
using PlatformPlatform.SharedKernel.Authentication;
using PlatformPlatform.SharedKernel.Tests;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using PlatformPlatform.SharedKernel.Validation;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.Signups;

public sealed class StartSignupTests : EndpointBaseTest<AccountManagementDbContext>
{
    [Fact]
    public async Task StartSignup_WhenSubdomainIsFreeAndEmailIsValid_ShouldReturnSuccess()
    {
        // Arrange
        var subdomain = Faker.Subdomain();
        var email = Faker.Internet.Email();
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
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("SignupStarted");
        TelemetryEventsCollectorSpy.CollectedEvents.Count(e =>
            e.GetType().Name == "SignupStarted" &&
            e.Properties["event.tenant_id"] == subdomain
        ).Should().Be(1);
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();

        await EmailService.Received(1).SendAsync(
            email.ToLower(),
            "Confirm your email address",
            Arg.Is<string>(s => s.Contains("Your confirmation code is below")),
            Arg.Any<CancellationToken>()
        );
    }

    [Theory]
    [InlineData("Subdomain empty", "")]
    [InlineData("Subdomain too short", "ab")]
    [InlineData("Subdomain too long", "1234567890123456789012345678901")]
    [InlineData("Subdomain with uppercase", "Tenant2")]
    [InlineData("Subdomain special characters", "tenant%2")]
    [InlineData("Subdomain with spaces", "tenant 2")]
    [InlineData("Subdomain ends with hyphen", "tenant-")]
    [InlineData("Subdomain starts with hyphen", "-tenant")]
    public async Task StartSignupCommand_WhenSubdomainInvalid_ShouldFail(string scenario, string subdomain)
    {
        // Arrange
        var email = Faker.Internet.Email();
        var command = new StartSignupCommand(subdomain, email);
        var mediator = Provider.GetRequiredService<ISender>();

        // Act
        var result = await mediator.Send(command);

        // Assert
        result.IsSuccess.Should().BeFalse(scenario);
        result.Errors?.Length.Should().Be(1, scenario);
        result.Errors![0].Code.Should().Be("Subdomain");
        result.Errors![0].Message.Should().Be("Subdomain must be between 3 to 30 lowercase letters, numbers, or hyphens.");
    }

    [Fact]
    public async Task StartSignup_WhenInvalidEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var subdomain = Faker.Subdomain();
        var invalidEmail = "invalid email";
        var command = new StartSignupCommand(subdomain, invalidEmail);

        // Act
        var response = await AnonymousHttpClient.PostAsJsonAsync("/api/account-management/signups/start", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("Email", "Email must be in a valid format and no longer than 100 characters.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
        await EmailService.DidNotReceive().SendAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), CancellationToken.None);
    }

    [Fact]
    public async Task StartSignup_WhenTenantExists_ShouldReturnBadRequest()
    {
        // Arrange
        var subdomain = DatabaseSeeder.Tenant1.Id;
        var email = Faker.Internet.Email();
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
        var subdomain = Faker.Subdomain();
        var email = Faker.Internet.Email();
        var command = new StartSignupCommand(subdomain, email);
        await AnonymousHttpClient.PostAsJsonAsync("/api/account-management/signups/start", command);

        // Act
        var response = await AnonymousHttpClient.PostAsJsonAsync("/api/account-management/signups/start", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Conflict, "Signup for this subdomain/mail has already been started. Please check your spam folder.");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1); // Only the first signup should create an event
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("SignupStarted");
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
        var subdomain = Faker.Subdomain();
        var email = Faker.Internet.Email();

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
}
