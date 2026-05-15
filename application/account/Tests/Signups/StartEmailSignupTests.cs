using System.Net;
using System.Net.Http.Json;
using Account.Database;
using Account.Features.EmailAuthentication.Commands;
using Account.Features.EmailAuthentication.Domain;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using SharedKernel.Authentication;
using SharedKernel.Integrations.Email;
using SharedKernel.Tests;
using SharedKernel.Tests.Persistence;
using SharedKernel.Validation;
using Xunit;

namespace Account.Tests.Signups;

public sealed class StartEmailSignupTests(AccountWebApplicationFactory factory) : EndpointBaseTest<AccountDbContext>(factory), IClassFixture<AccountWebApplicationFactory>
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
            Arg.Is<EmailMessage>(m =>
                m.Recipient == email.ToLower() &&
                m.Subject == "Confirm your email address" &&
                m.HtmlBody.Contains("Your confirmation code is below") &&
                m.HtmlBody.Contains("Enter it in your open browser window. It is only valid for a few minutes.") &&
                m.PlainTextBody.Contains("Your confirmation code is below") &&
                m.PlainTextBody.TrimEnd().Contains("@localhost #")
            ),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task StartSignup_WhenLocaleHeaderIsDanish_ShouldSendDanishConfirmationEmail()
    {
        // Anonymous signups have no User row to read Locale from, so the email locale comes from
        // the request culture - which UseRequestLocalization populates from the X-Locale header
        // sent by the SPA's API client (Accept-Language is a forbidden header in browsers, so the
        // SPA cannot set it from JS). Verifies the full chain: header → IRequestCultureFeature →
        // UserInfo.Locale → EmailTemplateBase.Locale → dist/StartSignup.da-DK.html.
        // Arrange
        var email = Faker.Internet.UniqueEmail();
        var command = new StartEmailSignupCommand(email);
        AnonymousHttpClient.DefaultRequestHeaders.Remove("X-Locale");
        AnonymousHttpClient.DefaultRequestHeaders.Add("X-Locale", "da-DK");

        // Act
        var response = await AnonymousHttpClient.PostAsJsonAsync("/api/account/authentication/email/signup/start", command);

        // Assert
        response.EnsureSuccessStatusCode();

        await EmailClient.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m =>
                m.Recipient == email.ToLower() &&
                m.Subject == "Bekræft din e-mailadresse" &&
                m.HtmlBody.Contains("Din bekræftelseskode står herunder") &&
                m.PlainTextBody.Contains("Din bekræftelseskode står herunder") &&
                m.PlainTextBody.TrimEnd().Contains("@localhost #")
            ),
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
        await EmailClient.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), CancellationToken.None);
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
            Connection.Insert("email_logins", [
                    ("id", EmailLoginId.NewId().ToString()),
                    ("created_at", TimeProvider.GetUtcNow().AddMinutes(-10)),
                    ("modified_at", null),
                    ("email", email),
                    ("type", nameof(EmailLoginType.Signup)),
                    ("one_time_password_hash", oneTimePasswordHash),
                    ("retry_count", 0),
                    ("resend_count", 0),
                    ("completed", false)
                ]
            );
        }

        var command = new StartEmailSignupCommand(email);

        // Act
        var response = await AnonymousHttpClient.PostAsJsonAsync("/api/account/authentication/email/signup/start", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.TooManyRequests, "Too many attempts to confirm this email address. Please try again later.");

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
        await EmailClient.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), CancellationToken.None);
    }
}
