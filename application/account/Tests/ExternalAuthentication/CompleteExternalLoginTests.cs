using System.Net;
using System.Text.Json;
using FluentAssertions;
using PlatformPlatform.Account.Features.ExternalAuthentication.Domain;
using PlatformPlatform.Account.Features.Tenants.Domain;
using PlatformPlatform.Account.Features.Users.Domain;
using PlatformPlatform.Account.Integrations.OAuth.Mock;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Tests.Persistence;
using Xunit;

namespace PlatformPlatform.Account.Tests.ExternalAuthentication;

public sealed class CompleteExternalLoginTests : ExternalAuthenticationTestBase
{
    [Fact]
    public async Task CompleteExternalLogin_WhenValid_ShouldCreateSessionAndRedirect()
    {
        // Arrange
        InsertUserWithExternalIdentity(MockOAuthProvider.MockEmail, ExternalProviderType.Google, MockOAuthProvider.MockProviderUserId);
        var (callbackUrl, cookies) = await StartLoginFlow("/dashboard");
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await CallCallback(callbackUrl, cookies);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/dashboard");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(2);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("SessionCreated");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("ExternalLoginCompleted");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteExternalLogin_WhenUserNotFound_ShouldRedirectToErrorPage()
    {
        // Arrange
        var (callbackUrl, cookies) = await StartLoginFlow();
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await CallCallback(callbackUrl, cookies);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain("/error?error=user_not_found");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("ExternalLoginFailed");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteExternalLogin_WhenIdentityMismatch_ShouldRedirectToErrorPage()
    {
        // Arrange
        InsertUserWithExternalIdentity(MockOAuthProvider.MockEmail, ExternalProviderType.Google, "different-provider-user-id");
        var (callbackUrl, cookies) = await StartLoginFlow();
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await CallCallback(callbackUrl, cookies);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain("/error?error=authentication_failed");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("ExternalLoginFailed");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteExternalLogin_WhenOAuthError_ShouldRedirectToErrorPage()
    {
        // Arrange
        var (callbackUrl, cookies) = await StartLoginFlow();
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await CallCallbackWithError(callbackUrl, cookies, "access_denied", "User denied access");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain("/error?error=access_denied");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("ExternalLoginFailed");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteExternalLogin_WhenMissingCode_ShouldRedirectToErrorPage()
    {
        // Arrange
        var (callbackUrl, cookies) = await StartLoginFlow();
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await CallCallbackWithoutCode(callbackUrl, cookies);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain("/error?error=authentication_failed");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("ExternalLoginFailed");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteExternalLogin_WhenMissingState_ShouldRedirectToErrorPage()
    {
        // Act
        var response = await NoRedirectHttpClient.GetAsync("/api/account/authentication/Google/login/callback");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain("/error?error=invalid_request");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("ExternalLoginFailed");
    }

    [Fact]
    public async Task CompleteExternalLogin_WhenFlowAlreadyCompleted_ShouldRedirectToErrorPage()
    {
        // Arrange
        InsertUserWithExternalIdentity(MockOAuthProvider.MockEmail, ExternalProviderType.Google, MockOAuthProvider.MockProviderUserId);
        var (callbackUrl, cookies) = await StartLoginFlow();
        await CallCallback(callbackUrl, cookies);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await CallCallback(callbackUrl, cookies);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain("/error?error=authentication_failed");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("ExternalLoginFailed");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteExternalLogin_WhenExpired_ShouldRedirectToErrorPage()
    {
        // Arrange
        var (callbackUrl, cookies) = await StartLoginFlow();
        var externalLoginId = GetExternalLoginIdFromUrl(callbackUrl);
        ExpireExternalLogin(externalLoginId);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await CallCallback(callbackUrl, cookies);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain("/error?error=session_expired");

        var loginResult = Connection.ExecuteScalar<string>(
            "SELECT LoginResult FROM ExternalLogins WHERE Id = @id", [new { id = externalLoginId }]
        );
        loginResult.Should().Be(nameof(ExternalLoginResult.LoginExpired));

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("ExternalLoginFailed");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteExternalLogin_WhenNonceMismatch_ShouldRedirectToErrorPage()
    {
        // Arrange
        InsertUserWithExternalIdentity(MockOAuthProvider.MockEmail, ExternalProviderType.Google, MockOAuthProvider.MockProviderUserId);
        var (callbackUrl, cookies) = await StartLoginFlow();
        var externalLoginId = GetExternalLoginIdFromUrl(callbackUrl);
        TamperWithNonce(externalLoginId);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await CallCallback(callbackUrl, cookies);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain("/error?error=authentication_failed");

        var loginResult = Connection.ExecuteScalar<string>(
            "SELECT LoginResult FROM ExternalLogins WHERE Id = @id", [new { id = externalLoginId }]
        );
        loginResult.Should().Be(nameof(ExternalLoginResult.NonceMismatch));

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("ExternalLoginFailed");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteExternalLogin_WhenUserHasNoExternalIdentity_ShouldLinkIdentityAndCreateSession()
    {
        // Arrange
        Connection.Insert("Users", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", UserId.NewId().ToString()),
                ("CreatedAt", TimeProvider.GetUtcNow()),
                ("ModifiedAt", null),
                ("Email", MockOAuthProvider.MockEmail),
                ("EmailConfirmed", true),
                ("FirstName", Faker.Name.FirstName()),
                ("LastName", Faker.Name.LastName()),
                ("Title", null),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Role", nameof(UserRole.Member)),
                ("Locale", "en-US"),
                ("ExternalIdentities", "[]")
            ]
        );
        var (callbackUrl, cookies) = await StartLoginFlow();
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await CallCallback(callbackUrl, cookies);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/");

        var externalIdentities = Connection.ExecuteScalar<string>(
            "SELECT ExternalIdentities FROM Users WHERE Email = @email", [new { email = MockOAuthProvider.MockEmail }]
        );
        externalIdentities.Should().Contain(MockOAuthProvider.MockProviderUserId);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(2);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("SessionCreated");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("ExternalLoginCompleted");
        TelemetryEventsCollectorSpy.AreAllEventsDispatched.Should().BeTrue();
    }

    [Fact]
    public async Task CompleteExternalLogin_WhenInvitedUserHasNoName_ShouldUpdateNameFromGoogleProfile()
    {
        // Arrange
        Connection.Insert("Users", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", UserId.NewId().ToString()),
                ("CreatedAt", TimeProvider.GetUtcNow()),
                ("ModifiedAt", null),
                ("Email", MockOAuthProvider.MockEmail),
                ("EmailConfirmed", false),
                ("FirstName", null),
                ("LastName", null),
                ("Title", null),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Role", nameof(UserRole.Member)),
                ("Locale", "en-US"),
                ("ExternalIdentities", "[]")
            ]
        );
        var (callbackUrl, cookies) = await StartLoginFlow();
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await CallCallback(callbackUrl, cookies);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/");

        var firstName = Connection.ExecuteScalar<string>(
            "SELECT FirstName FROM Users WHERE Email = @email", [new { email = MockOAuthProvider.MockEmail }]
        );
        var lastName = Connection.ExecuteScalar<string>(
            "SELECT LastName FROM Users WHERE Email = @email", [new { email = MockOAuthProvider.MockEmail }]
        );
        firstName.Should().Be(MockOAuthProvider.MockFirstName);
        lastName.Should().Be(MockOAuthProvider.MockLastName);
    }

    [Fact]
    public async Task CompleteExternalLogin_WhenUserAlreadyHasName_ShouldNotOverwriteFromGoogleProfile()
    {
        // Arrange
        var existingFirstName = Faker.Name.FirstName();
        var existingLastName = Faker.Name.LastName();
        Connection.Insert("Users", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", UserId.NewId().ToString()),
                ("CreatedAt", TimeProvider.GetUtcNow()),
                ("ModifiedAt", null),
                ("Email", MockOAuthProvider.MockEmail),
                ("EmailConfirmed", true),
                ("FirstName", existingFirstName),
                ("LastName", existingLastName),
                ("Title", null),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Role", nameof(UserRole.Member)),
                ("Locale", "en-US"),
                ("ExternalIdentities", "[]")
            ]
        );
        var (callbackUrl, cookies) = await StartLoginFlow();
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await CallCallback(callbackUrl, cookies);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/");

        var firstName = Connection.ExecuteScalar<string>(
            "SELECT FirstName FROM Users WHERE Email = @email", [new { email = MockOAuthProvider.MockEmail }]
        );
        var lastName = Connection.ExecuteScalar<string>(
            "SELECT LastName FROM Users WHERE Email = @email", [new { email = MockOAuthProvider.MockEmail }]
        );
        firstName.Should().Be(existingFirstName);
        lastName.Should().Be(existingLastName);
    }

    [Fact]
    public async Task CompleteExternalLogin_WhenNoCookie_ShouldRedirectToErrorPage()
    {
        // Arrange
        var (callbackUrl, _) = await StartLoginFlow();
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await CallCallback(callbackUrl, []);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain("/error");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("ExternalLoginFailed");
    }

    [Fact]
    public async Task CompleteExternalLogin_WhenDefaultReturnPath_ShouldRedirectToRoot()
    {
        // Arrange
        InsertUserWithExternalIdentity(MockOAuthProvider.MockEmail, ExternalProviderType.Google, MockOAuthProvider.MockProviderUserId);
        var (callbackUrl, cookies) = await StartLoginFlow();
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await CallCallback(callbackUrl, cookies);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/");
    }

    [Fact]
    public async Task CompleteExternalLogin_WhenValid_ShouldMarkCompletedInDatabase()
    {
        // Arrange
        InsertUserWithExternalIdentity(MockOAuthProvider.MockEmail, ExternalProviderType.Google, MockOAuthProvider.MockProviderUserId);
        var (callbackUrl, cookies) = await StartLoginFlow();
        var externalLoginId = GetExternalLoginIdFromUrl(callbackUrl);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        await CallCallback(callbackUrl, cookies);

        // Assert
        var loginResult = Connection.ExecuteScalar<string>(
            "SELECT LoginResult FROM ExternalLogins WHERE Id = @id", [new { id = externalLoginId }]
        );
        loginResult.Should().Be(nameof(ExternalLoginResult.Success));
    }

    [Fact]
    public async Task CompleteExternalLogin_WhenUserNotFound_ShouldMarkFailedInDatabase()
    {
        // Arrange
        var (callbackUrl, cookies) = await StartLoginFlow();
        var externalLoginId = GetExternalLoginIdFromUrl(callbackUrl);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        await CallCallback(callbackUrl, cookies);

        // Assert
        var loginResult = Connection.ExecuteScalar<string>(
            "SELECT LoginResult FROM ExternalLogins WHERE Id = @id", [new { id = externalLoginId }]
        );
        loginResult.Should().Be(nameof(ExternalLoginResult.UserNotFound));
    }

    [Fact]
    public async Task CompleteExternalLogin_WithValidPreferredTenant_ShouldLoginToPreferredTenant()
    {
        // Arrange
        var tenant2Id = TenantId.NewId();
        var user2Id = UserId.NewId();

        Connection.Insert("Tenants", [
                ("Id", tenant2Id.Value),
                ("CreatedAt", TimeProvider.GetUtcNow()),
                ("ModifiedAt", null),
                ("Name", Faker.Company.CompanyName()),
                ("State", nameof(TenantState.Active)),
                ("Logo", """{"Url":null,"Version":0}""")
            ]
        );

        var identities = JsonSerializer.Serialize(new[] { new { Provider = nameof(ExternalProviderType.Google), ProviderUserId = MockOAuthProvider.MockProviderUserId } });
        Connection.Insert("Users", [
                ("TenantId", DatabaseSeeder.Tenant1.Id.ToString()),
                ("Id", UserId.NewId().ToString()),
                ("CreatedAt", TimeProvider.GetUtcNow()),
                ("ModifiedAt", null),
                ("Email", MockOAuthProvider.MockEmail),
                ("EmailConfirmed", true),
                ("FirstName", Faker.Name.FirstName()),
                ("LastName", Faker.Name.LastName()),
                ("Title", null),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Role", nameof(UserRole.Member)),
                ("Locale", "en-US"),
                ("ExternalIdentities", identities)
            ]
        );

        Connection.Insert("Users", [
                ("TenantId", tenant2Id.Value),
                ("Id", user2Id.ToString()),
                ("CreatedAt", TimeProvider.GetUtcNow()),
                ("ModifiedAt", null),
                ("Email", MockOAuthProvider.MockEmail),
                ("EmailConfirmed", true),
                ("FirstName", Faker.Name.FirstName()),
                ("LastName", Faker.Name.LastName()),
                ("Title", null),
                ("Avatar", JsonSerializer.Serialize(new Avatar())),
                ("Role", nameof(UserRole.Owner)),
                ("Locale", "en-US"),
                ("ExternalIdentities", identities)
            ]
        );

        var (callbackUrl, cookies) = await StartLoginFlow(preferredTenantId: tenant2Id);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await CallCallback(callbackUrl, cookies);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/");

        var sessionTenantId = Connection.ExecuteScalar<long>(
            "SELECT TenantId FROM Sessions WHERE UserId = @userId ORDER BY CreatedAt DESC LIMIT 1", [new { userId = user2Id.ToString() }]
        );
        sessionTenantId.Should().Be(tenant2Id.Value);

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(2);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("SessionCreated");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("ExternalLoginCompleted");
        TelemetryEventsCollectorSpy.CollectedEvents[1].Properties["event.user_id"].Should().Be(user2Id);
    }

    [Fact]
    public async Task CompleteExternalLogin_WhenTamperedState_ShouldRedirectToErrorPage()
    {
        // Arrange
        var (callbackUrl, cookies) = await StartLoginFlow();
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await CallCallbackWithTamperedState(callbackUrl, cookies);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain("/error?error=invalid_request");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("ExternalLoginFailed");
    }

    [Fact]
    public async Task CompleteExternalLogin_WhenFlowIdMismatch_ShouldRedirectToErrorPage()
    {
        // Arrange
        var (callbackUrl1, _) = await StartLoginFlow();
        var (_, cookies2) = await StartLoginFlow();
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
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("ExternalLoginFailed");
    }

    [Fact]
    public async Task CompleteExternalLogin_WhenTamperedCookie_ShouldRedirectToErrorPage()
    {
        // Arrange
        var (callbackUrl, _) = await StartLoginFlow();
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await CallCallbackWithTamperedCookie(callbackUrl, "corrupted-encrypted-data");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Contain("/error");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("ExternalLoginFailed");
    }

    [Fact]
    public async Task CompleteExternalLogin_WithInvalidPreferredTenant_ShouldLoginToDefaultTenant()
    {
        // Arrange
        var invalidTenantId = TenantId.NewId();
        var userId = InsertUserWithExternalIdentity(MockOAuthProvider.MockEmail, ExternalProviderType.Google, MockOAuthProvider.MockProviderUserId);
        var (callbackUrl, cookies) = await StartLoginFlow(preferredTenantId: invalidTenantId);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await CallCallback(callbackUrl, cookies);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(2);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("SessionCreated");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("ExternalLoginCompleted");
        TelemetryEventsCollectorSpy.CollectedEvents[1].Properties["event.user_id"].Should().Be(userId);
    }

    [Fact]
    public async Task CompleteExternalLogin_WithPreferredTenantUserDoesNotHaveAccess_ShouldLoginToDefaultTenant()
    {
        // Arrange
        var tenant2Id = TenantId.NewId();

        Connection.Insert("Tenants", [
                ("Id", tenant2Id.Value),
                ("CreatedAt", TimeProvider.GetUtcNow()),
                ("ModifiedAt", null),
                ("Name", Faker.Company.CompanyName()),
                ("State", nameof(TenantState.Active)),
                ("Logo", """{"Url":null,"Version":0}""")
            ]
        );

        var userId = InsertUserWithExternalIdentity(MockOAuthProvider.MockEmail, ExternalProviderType.Google, MockOAuthProvider.MockProviderUserId);
        var (callbackUrl, cookies) = await StartLoginFlow(preferredTenantId: tenant2Id);
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await CallCallback(callbackUrl, cookies);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.ToString().Should().Be("/");

        TelemetryEventsCollectorSpy.CollectedEvents.Count.Should().Be(2);
        TelemetryEventsCollectorSpy.CollectedEvents[0].GetType().Name.Should().Be("SessionCreated");
        TelemetryEventsCollectorSpy.CollectedEvents[1].GetType().Name.Should().Be("ExternalLoginCompleted");
        TelemetryEventsCollectorSpy.CollectedEvents[1].Properties["event.user_id"].Should().Be(userId);
    }
}
