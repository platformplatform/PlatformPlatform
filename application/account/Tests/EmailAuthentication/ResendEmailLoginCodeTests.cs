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
using Xunit;

namespace Account.Tests.EmailAuthentication;

public sealed class ResendEmailLoginCodeTests(AccountWebApplicationFactory factory) : EndpointBaseTest<AccountDbContext>(factory), IClassFixture<AccountWebApplicationFactory>
{
    [Fact]
    public async Task ResendEmailLoginCode_WhenValid_ShouldSendNewVerificationCode()
    {
        // Arrange
        var email = DatabaseSeeder.Tenant1Owner.Email;
        var emailLoginId = await StartEmailLogin(email);
        EmailClient.ClearReceivedCalls();
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AnonymousHttpClient.PostAsJsonAsync(
            $"/api/account/authentication/email/login/{emailLoginId}/resend-code", new { }
        );

        // Assert
        response.EnsureSuccessStatusCode();
        var responseBody = await response.DeserializeResponse<ResendEmailLoginCodeResponse>();
        responseBody.Should().NotBeNull();
        responseBody.ValidForSeconds.Should().Be(300);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("EmailLoginCodeResend");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();

        await EmailClient.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m =>
                m.Recipient == email.ToLower() &&
                m.Subject == "Your verification code (resend)" &&
                m.HtmlBody.Contains("your new verification code") &&
                m.PlainTextBody.Contains("Here's your new verification code") &&
                m.PlainTextBody.Contains("We're sending this code again as you requested.") &&
                m.PlainTextBody.TrimEnd().Contains("@localhost #")
            ),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task ResendEmailLoginCode_WhenUserHasDanishLocale_ShouldSendDanishResendEmail()
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
        var emailLoginId = await StartEmailLogin(email);
        EmailClient.ClearReceivedCalls();
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AnonymousHttpClient.PostAsJsonAsync(
            $"/api/account/authentication/email/login/{emailLoginId}/resend-code", new { }
        );

        // Assert
        response.EnsureSuccessStatusCode();

        await EmailClient.Received(1).SendAsync(
            Arg.Is<EmailMessage>(m =>
                m.Recipient == email &&
                m.Subject == "Din bekræftelseskode (gensendt)" &&
                m.HtmlBody.Contains("Her er din nye bekræftelseskode") &&
                m.PlainTextBody.Contains("Her er din nye bekræftelseskode") &&
                m.PlainTextBody.TrimEnd().Contains("@localhost #")
            ),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task ResendEmailLoginCode_WhenAlreadyResentOnce_ShouldReturnForbidden()
    {
        // Arrange
        var email = DatabaseSeeder.Tenant1Owner.Email;
        var emailLoginId = await StartEmailLogin(email);
        await AnonymousHttpClient.PostAsJsonAsync(
            $"/api/account/authentication/email/login/{emailLoginId}/resend-code", new { }
        );
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AnonymousHttpClient.PostAsJsonAsync(
            $"/api/account/authentication/email/login/{emailLoginId}/resend-code", new { }
        );

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.Forbidden, "Too many attempts, please request a new code.");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("EmailLoginCodeResendBlocked");
    }

    [Fact]
    public async Task ResendEmailLoginCode_WhenIdNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var unknownId = EmailLoginId.NewId();

        // Act
        var response = await AnonymousHttpClient.PostAsJsonAsync(
            $"/api/account/authentication/email/login/{unknownId}/resend-code", new { }
        );

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.NotFound, $"Email login with id '{unknownId}' not found.");
        await EmailClient.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ResendEmailLoginCode_WhenAlreadyCompleted_ShouldReturnBadRequest()
    {
        // Arrange
        var email = DatabaseSeeder.Tenant1Owner.Email;
        var oneTimePasswordHash = new PasswordHasher<object>().HashPassword(this, OneTimePasswordHelper.GenerateOneTimePassword(6));
        var emailLoginId = EmailLoginId.NewId();
        Connection.Insert("email_logins", [
                ("id", emailLoginId.ToString()),
                ("created_at", TimeProvider.GetUtcNow()),
                ("modified_at", null),
                ("email", email.ToLower()),
                ("type", nameof(EmailLoginType.Login)),
                ("one_time_password_hash", oneTimePasswordHash),
                ("retry_count", 0),
                ("resend_count", 0),
                ("completed", true)
            ]
        );

        // Act
        var response = await AnonymousHttpClient.PostAsJsonAsync(
            $"/api/account/authentication/email/login/{emailLoginId}/resend-code", new { }
        );

        // Assert
        await response.ShouldHaveErrorStatusCode(
            HttpStatusCode.BadRequest, $"The email login with id '{emailLoginId}' has already been completed."
        );
        await EmailClient.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    private async Task<EmailLoginId> StartEmailLogin(string email)
    {
        var response = await AnonymousHttpClient.PostAsJsonAsync(
            "/api/account/authentication/email/login/start", new StartEmailLoginCommand(email)
        );
        response.EnsureSuccessStatusCode();
        var responseBody = await response.DeserializeResponse<StartEmailLoginResponse>();
        return responseBody!.EmailLoginId;
    }
}
