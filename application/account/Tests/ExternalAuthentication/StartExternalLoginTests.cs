using System.Net;
using FluentAssertions;
using PlatformPlatform.Account.Features.ExternalAuthentication.Domain;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using Xunit;

namespace PlatformPlatform.Account.Tests.ExternalAuthentication;

public sealed class StartExternalLoginTests : ExternalAuthenticationTestBase
{
    [Fact]
    public async Task StartExternalLogin_WhenValidProvider_ShouldRedirectToAuthorizationUrl()
    {
        // Act
        var response = await NoRedirectHttpClient.GetAsync(
            "/api/account/authentication/Google/login/start?returnPath=%2Fdashboard&locale=en-US"
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var location = response.Headers.Location!.ToString();
        location.Should().Contain("/api/account/authentication/Google/login/callback");
        location.Should().Contain("code=mock-authorization-code");
        location.Should().Contain("state=");

        var externalLoginId = GetExternalLoginIdFromResponse(response);
        Connection.RowExists("ExternalLogins", externalLoginId).Should().BeTrue();

        var loginType = Connection.ExecuteScalar<string>(
            "SELECT Type FROM ExternalLogins WHERE Id = @id", [new { id = externalLoginId }]
        );
        loginType.Should().Be(nameof(ExternalLoginType.Login));

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("ExternalLoginStarted");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task StartExternalLogin_WhenNullReturnPathAndLocale_ShouldRedirectToAuthorizationUrl()
    {
        // Act
        var response = await NoRedirectHttpClient.GetAsync(
            "/api/account/authentication/Google/login/start"
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        var location = response.Headers.Location!.ToString();
        location.Should().Contain("code=mock-authorization-code");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("ExternalLoginStarted");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }
}
