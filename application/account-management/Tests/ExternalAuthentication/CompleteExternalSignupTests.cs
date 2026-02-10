using System.Net;
using FluentAssertions;
using PlatformPlatform.AccountManagement.Features.ExternalAuthentication.Domain;
using PlatformPlatform.AccountManagement.Integrations.OAuth.Mock;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.ExternalAuthentication;

public sealed class CompleteExternalSignupTests : ExternalAuthenticationTestBase
{
    [Fact]
    public async Task CompleteExternalSignup_WhenValid_ShouldCreateTenantUserAndSession()
    {
        // Arrange
        var (callbackUrl, cookies) = await StartSignupFlow("/onboarding", "en-US");
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await CallCallback(callbackUrl, cookies, "signup");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/onboarding");

        var userCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM Users WHERE Email = @email", [new { email = MockOAuthProvider.MockEmail }]
        );
        userCount.Should().Be(1);

        var tenantCount = Connection.ExecuteScalar<long>("SELECT COUNT(*) FROM Tenants", []);
        tenantCount.Should().BeGreaterThan(1);

        TelemetryEventsCollectorSpy.CollectedEvents.Should().Contain(e => e.GetType().Name == "TenantCreated");
        TelemetryEventsCollectorSpy.CollectedEvents.Should().Contain(e => e.GetType().Name == "UserCreated");
        TelemetryEventsCollectorSpy.CollectedEvents.Should().Contain(e => e.GetType().Name == "SessionCreated");
        TelemetryEventsCollectorSpy.CollectedEvents.Should().Contain(e => e.GetType().Name == "ExternalSignupCompleted");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteExternalSignup_WhenUserAlreadyExists_ShouldRedirectToErrorPage()
    {
        // Arrange
        InsertUserWithExternalIdentity(MockOAuthProvider.MockEmail, ExternalProviderType.Google, MockOAuthProvider.MockProviderUserId);
        var (callbackUrl, cookies) = await StartSignupFlow();
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await CallCallback(callbackUrl, cookies, "signup");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain("/error?error=account_already_exists");

        var externalLoginId = GetExternalLoginIdFromUrl(callbackUrl);
        var loginResult = Connection.ExecuteScalar<string>(
            "SELECT LoginResult FROM ExternalLogins WHERE Id = @id", [new { id = externalLoginId }]
        );
        loginResult.Should().Be(nameof(ExternalLoginResult.AccountAlreadyExists));

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("ExternalSignupFailed");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteExternalSignup_WhenOAuthError_ShouldRedirectToErrorPage()
    {
        // Arrange
        var (callbackUrl, cookies) = await StartSignupFlow();
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await CallCallbackWithError(callbackUrl, cookies, "access_denied", "User denied access");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain("/error?error=access_denied");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("ExternalSignupFailed");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteExternalSignup_WhenMissingCode_ShouldRedirectToErrorPage()
    {
        // Arrange
        var (callbackUrl, cookies) = await StartSignupFlow();
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await CallCallbackWithoutCode(callbackUrl, cookies);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain("/error?error=authentication_failed");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("ExternalSignupFailed");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteExternalSignup_WhenMissingState_ShouldRedirectToErrorPage()
    {
        // Act
        var response = await NoRedirectHttpClient.GetAsync("/api/account-management/authentication/Google/signup/callback");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain("/error?error=invalid_request");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("ExternalSignupFailed");
    }

    [Fact]
    public async Task CompleteExternalSignup_WhenExpired_ShouldRedirectToErrorPage()
    {
        // Arrange
        var (callbackUrl, cookies) = await StartSignupFlow();
        var externalLoginId = GetExternalLoginIdFromUrl(callbackUrl);
        ExpireExternalLogin(externalLoginId);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await CallCallback(callbackUrl, cookies, "signup");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain("/error?error=session_expired");

        var loginResult = Connection.ExecuteScalar<string>(
            "SELECT LoginResult FROM ExternalLogins WHERE Id = @id", [new { id = externalLoginId }]
        );
        loginResult.Should().Be(nameof(ExternalLoginResult.LoginExpired));

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("ExternalSignupFailed");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteExternalSignup_WhenNonceMismatch_ShouldRedirectToErrorPage()
    {
        // Arrange
        var (callbackUrl, cookies) = await StartSignupFlow();
        var externalLoginId = GetExternalLoginIdFromUrl(callbackUrl);
        TamperWithNonce(externalLoginId);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await CallCallback(callbackUrl, cookies, "signup");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain("/error?error=authentication_failed");

        var loginResult = Connection.ExecuteScalar<string>(
            "SELECT LoginResult FROM ExternalLogins WHERE Id = @id", [new { id = externalLoginId }]
        );
        loginResult.Should().Be(nameof(ExternalLoginResult.NonceMismatch));

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("ExternalSignupFailed");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteExternalSignup_WhenTamperedState_ShouldRedirectToErrorPage()
    {
        // Arrange
        var (callbackUrl, cookies) = await StartSignupFlow();
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await CallCallbackWithTamperedState(callbackUrl, cookies);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain("/error?error=invalid_request");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("ExternalSignupFailed");
    }

    [Fact]
    public async Task CompleteExternalSignup_WhenFlowIdMismatch_ShouldRedirectToErrorPage()
    {
        // Arrange
        var (callbackUrl1, _) = await StartSignupFlow();
        var (_, cookies2) = await StartSignupFlow();
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await CallCallbackWithCrossedFlows(callbackUrl1, cookies2);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain("/error?error=authentication_failed");

        var externalLoginId = GetExternalLoginIdFromUrl(callbackUrl1);
        var loginResult = Connection.ExecuteScalar<string>(
            "SELECT LoginResult FROM ExternalLogins WHERE Id = @id", [new { id = externalLoginId }]
        );
        loginResult.Should().Be(nameof(ExternalLoginResult.FlowIdMismatch));

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("ExternalSignupFailed");
    }

    [Fact]
    public async Task CompleteExternalSignup_WhenTamperedCookie_ShouldRedirectToErrorPage()
    {
        // Arrange
        var (callbackUrl, _) = await StartSignupFlow();
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await CallCallbackWithTamperedCookie(callbackUrl, "corrupted-encrypted-data");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain("/error");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("ExternalSignupFailed");
    }

    [Fact]
    public async Task CompleteExternalSignup_WhenFlowAlreadyCompleted_ShouldRedirectToErrorPage()
    {
        // Arrange
        var (callbackUrl, cookies) = await StartSignupFlow();
        await CallCallback(callbackUrl, cookies, "signup");
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await CallCallback(callbackUrl, cookies, "signup");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain("/error?error=authentication_failed");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("ExternalSignupFailed");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteExternalSignup_WhenNoCookie_ShouldRedirectToErrorPage()
    {
        // Arrange
        var (callbackUrl, _) = await StartSignupFlow();
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await CallCallback(callbackUrl, [], "signup");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain("/error");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("ExternalSignupFailed");
    }

    [Fact]
    public async Task CompleteExternalSignup_WhenValid_ShouldMarkCompletedInDatabase()
    {
        // Arrange
        var (callbackUrl, cookies) = await StartSignupFlow(locale: "en-US");
        var externalLoginId = GetExternalLoginIdFromUrl(callbackUrl);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        await CallCallback(callbackUrl, cookies, "signup");

        // Assert
        var loginResult = Connection.ExecuteScalar<string>(
            "SELECT LoginResult FROM ExternalLogins WHERE Id = @id", [new { id = externalLoginId }]
        );
        loginResult.Should().Be(nameof(ExternalLoginResult.Success));
    }

    [Fact]
    public async Task CompleteExternalSignup_WhenValid_ShouldLinkExternalIdentity()
    {
        // Arrange
        var (callbackUrl, cookies) = await StartSignupFlow(locale: "en-US");
        TelemetryEventsCollectorSpy.Reset();

        // Act
        await CallCallback(callbackUrl, cookies, "signup");

        // Assert
        var externalIdentities = Connection.ExecuteScalar<string>(
            "SELECT ExternalIdentities FROM Users WHERE Email = @email", [new { email = MockOAuthProvider.MockEmail }]
        );
        externalIdentities.Should().Contain(MockOAuthProvider.MockProviderUserId);
    }

    [Fact]
    public async Task CompleteExternalSignup_WhenDefaultReturnPath_ShouldRedirectToRoot()
    {
        // Arrange
        var (callbackUrl, cookies) = await StartSignupFlow();
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await CallCallback(callbackUrl, cookies, "signup");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/");
    }
}
