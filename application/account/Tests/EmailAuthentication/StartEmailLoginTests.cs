using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Account.Database;
using Account.Features.EmailAuthentication.Commands;
using Account.Features.EmailAuthentication.Domain;
using Account.Features.Users.Domain;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using SharedKernel.Authentication;
using SharedKernel.Domain;
using SharedKernel.Integrations.Email;
using SharedKernel.Tests;
using SharedKernel.Tests.Persistence;
using SharedKernel.Validation;
using Xunit;

namespace Account.Tests.EmailAuthentication;

public sealed class StartEmailLoginTests(AccountWebApplicationFactory factory) : EndpointBaseTest<AccountDbContext>(factory), IClassFixture<AccountWebApplicationFactory>
{
    [Fact]
    public async Task StartEmailLogin_WhenValidEmailAndUserExists_ShouldReturnSuccess()
    {
        // Arrange
        var email = DatabaseSeeder.Tenant1Owner.Email;
        var command = new StartEmailLoginCommand(email);

        // Act
        var response = await AnonymousHttpClient.PostAsJsonAsync("/api/account/authentication/email/login/start", command);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseBody = await response.DeserializeResponse<StartEmailLoginResponse>();
        responseBody.Should().NotBeNull();
        responseBody.ValidForSeconds.Should().Be(300);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("EmailLoginStarted");
        TelemetryEventsCollectorSpy.CollectedEvents[0].Properties["event.user_id"].Should().Be(DatabaseSeeder.Tenant1Owner.Id);
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();

        await EmailClient.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m =>
                m.Recipient == email.ToLower() &&
                m.Subject == "PlatformPlatform login verification code" &&
                m.HtmlBody.Contains("Your confirmation code is below") &&
                m.HtmlBody.Contains("Enter it in your open browser window. It is only valid for a few minutes.") &&
                m.PlainTextBody.Contains("Your confirmation code is below") &&
                m.PlainTextBody.Contains("Enter it in your open browser window. It is only valid for a few minutes.") &&
                m.PlainTextBody.TrimEnd().Contains("@localhost #")
            ),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task StartEmailLogin_WhenUserHasDanishLocale_ShouldSendDanishLoginEmail()
    {
        // Arrange
        var email = Faker.Internet.UniqueEmail().ToLowerInvariant();
        Connection.Insert("users", [
                ("tenant_id", DatabaseSeeder.Tenant1.Id.ToString()),
                ("id", UserId.NewId().ToString()),
                ("created_at", TimeProvider.GetUtcNow().AddDays(-30)),
                ("modified_at", null),
                ("deleted_at", null),
                ("email", email),
                ("first_name", Faker.Person.FirstName),
                ("last_name", Faker.Person.LastName),
                ("title", null),
                ("role", nameof(UserRole.Member)),
                ("email_confirmed", true),
                ("avatar", JsonSerializer.Serialize(new Avatar())),
                ("locale", "da-DK"),
                ("external_identities", "[]"),
                ("rollout_bucket", 50)
            ]
        );
        var command = new StartEmailLoginCommand(email);

        // Act
        var response = await AnonymousHttpClient.PostAsJsonAsync("/api/account/authentication/email/login/start", command);

        // Assert
        response.EnsureSuccessStatusCode();

        await EmailClient.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m =>
                m.Recipient == email &&
                m.Subject == "PlatformPlatform-bekræftelseskode til login" &&
                m.HtmlBody.Contains("Din bekræftelseskode står herunder") &&
                m.PlainTextBody.Contains("Din bekræftelseskode står herunder") &&
                m.PlainTextBody.TrimEnd().Contains("@localhost #")
            ),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task StartEmailLoginCommand_WhenEmailIsEmpty_ShouldFail()
    {
        // Arrange
        var command = new StartEmailLoginCommand("");

        // Act
        var response = await AnonymousHttpClient.PostAsJsonAsync("/api/account/authentication/email/login/start", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("email", "Email must be in a valid format and no longer than 100 characters.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
        await EmailClient.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("Invalid Email Format", "invalid-email")]
    [InlineData("Email Too Long", "abcdefghijklmnopqrstuvwyz0123456789-abcdefghijklmnopqrstuvwyz0123456789-abcdefghijklmnopqrstuvwyz0123456789@example.com")]
    [InlineData("Double Dots In Domain", "neo@gmail..com")]
    [InlineData("Comma Instead Of Dot", "q@q,com")]
    [InlineData("Space In Domain", "tje@mentum .dk")]
    public async Task StartEmailLoginCommand_WhenEmailInvalid_ShouldFail(string scenario, string invalidEmail)
    {
        // Arrange
        var command = new StartEmailLoginCommand(invalidEmail);

        // Act
        var response = await AnonymousHttpClient.PostAsJsonAsync("/api/account/authentication/email/login/start", command);

        // Assert
        var expectedErrors = new[]
        {
            new ErrorDetail("email", "Email must be in a valid format and no longer than 100 characters.")
        };
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, expectedErrors);

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse(scenario);
        await EmailClient.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartEmailLoginCommand_WhenUserDoesNotExist_ShouldReturnFakeEmailLoginId()
    {
        // Arrange
        var email = Faker.Internet.UniqueEmail();
        var command = new StartEmailLoginCommand(email);

        // Act
        var response = await AnonymousHttpClient.PostAsJsonAsync("/api/account/authentication/email/login/start", command);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseBody = await response.DeserializeResponse<StartEmailLoginResponse>();
        responseBody.Should().NotBeNull();
        responseBody.ValidForSeconds.Should().Be(300);

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();

        await EmailClient.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m =>
                m.Recipient == email.ToLower() &&
                m.Subject == "No account found" &&
                m.HtmlBody.Contains("Is this the right email address?") &&
                m.HtmlBody.Contains("PlatformPlatform account tied to") &&
                m.PlainTextBody.Contains("Is this the right email address?") &&
                m.PlainTextBody.Contains(email.ToLower())
            ),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task StartEmailLogin_WhenTooManyAttempts_ShouldReturnTooManyRequests()
    {
        // Arrange
        var email = DatabaseSeeder.Tenant1Owner.Email;

        for (var i = 1; i <= 4; i++)
        {
            var oneTimePasswordHash = new PasswordHasher<object>().HashPassword(this, OneTimePasswordHelper.GenerateOneTimePassword(6));
            Connection.Insert("email_logins", [
                    ("id", EmailLoginId.NewId().ToString()),
                    ("created_at", TimeProvider.GetUtcNow().AddMinutes(-10)),
                    ("modified_at", null),
                    ("email", email.ToLower()),
                    ("type", nameof(EmailLoginType.Login)),
                    ("one_time_password_hash", oneTimePasswordHash),
                    ("retry_count", 0),
                    ("resend_count", 0),
                    ("completed", false)
                ]
            );
        }

        var command = new StartEmailLoginCommand(email);

        // Act
        var response = await AnonymousHttpClient.PostAsJsonAsync("/api/account/authentication/email/login/start", command);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.TooManyRequests, "Too many attempts to confirm this email address. Please try again later.");

        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeFalse();
        await EmailClient.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), CancellationToken.None);
    }

    [Fact]
    public async Task StartEmailLogin_WhenUserIsSoftDeleted_ShouldReturnFakeEmailLoginIdAndSendUnknownUserEmail()
    {
        // Arrange
        var email = Faker.Internet.UniqueEmail();
        Connection.Insert("users", [
                ("tenant_id", DatabaseSeeder.Tenant1.Id.ToString()),
                ("id", UserId.NewId().ToString()),
                ("created_at", TimeProvider.GetUtcNow().AddDays(-30)),
                ("modified_at", TimeProvider.GetUtcNow().AddDays(-1)),
                ("deleted_at", TimeProvider.GetUtcNow().AddDays(-1)),
                ("email", email.ToLower()),
                ("first_name", Faker.Person.FirstName),
                ("last_name", Faker.Person.LastName),
                ("title", "Former Employee"),
                ("role", nameof(UserRole.Member)),
                ("email_confirmed", true),
                ("avatar", JsonSerializer.Serialize(new Avatar())),
                ("locale", "en-US"),
                ("external_identities", "[]"),
                ("rollout_bucket", 42)
            ]
        );

        var command = new StartEmailLoginCommand(email);

        // Act
        var response = await AnonymousHttpClient.PostAsJsonAsync("/api/account/authentication/email/login/start", command);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseBody = await response.DeserializeResponse<StartEmailLoginResponse>();
        responseBody.Should().NotBeNull();
        responseBody.ValidForSeconds.Should().Be(300);

        TelemetryEventsCollectorSpy.CollectedEvents.Should().BeEmpty();

        await EmailClient.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m =>
                m.Recipient == email.ToLower() &&
                m.Subject == "No account found" &&
                m.HtmlBody.Contains("Is this the right email address?") &&
                m.HtmlBody.Contains("PlatformPlatform account tied to") &&
                m.PlainTextBody.Contains("Is this the right email address?") &&
                m.PlainTextBody.Contains(email.ToLower())
            ),
            Arg.Any<CancellationToken>()
        );
    }
}
