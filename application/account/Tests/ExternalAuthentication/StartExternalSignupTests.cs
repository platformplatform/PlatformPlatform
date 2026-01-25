using System.Net;
using FluentAssertions;
using PlatformPlatform.Account.Features.ExternalAuthentication.Domain;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using Xunit;

namespace PlatformPlatform.Account.Tests.ExternalAuthentication;

public sealed class StartExternalSignupTests : ExternalAuthenticationTestBase
{
    [Fact]
    public async Task StartExternalSignup_WhenValidProvider_ShouldRedirectToAuthorizationUrl()
    {
        // Act
        var response = await NoRedirectHttpClient.GetAsync(
            "/api/account/authentication/Google/signup/start?returnPath=%2Fonboarding&locale=en-US"
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var location = response.Headers.Location!.ToString();
        location.Should().Contain("/api/account/authentication/Google/signup/callback");
        location.Should().Contain("code=mock-authorization-code");
        location.Should().Contain("state=");

        var externalLoginId = GetExternalLoginIdFromResponse(response);
        Connection.RowExists("ExternalLogins", externalLoginId).Should().BeTrue();

        var loginType = Connection.ExecuteScalar<string>(
            "SELECT Type FROM ExternalLogins WHERE Id = @id", [new { id = externalLoginId }]
        );
        loginType.Should().Be(nameof(ExternalLoginType.Signup));

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("ExternalSignupStarted");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task StartExternalSignup_WhenNullReturnPathAndLocale_ShouldRedirectToAuthorizationUrl()
    {
        // Act
        var response = await NoRedirectHttpClient.GetAsync(
            "/api/account/authentication/Google/signup/start"
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var location = response.Headers.Location!.ToString();
        location.Should().Contain("code=mock-authorization-code");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("ExternalSignupStarted");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }
}
