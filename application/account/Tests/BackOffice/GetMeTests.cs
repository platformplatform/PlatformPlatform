using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Account.Features.BackOffice.Queries;
using FluentAssertions;
using SharedKernel.Authentication.BackOfficeIdentity;
using SharedKernel.Authentication.MockEasyAuth;
using Xunit;

namespace Account.Tests.BackOffice;

public sealed class GetMeTests(BackOfficeWebApplicationFactory factory) : BackOfficeEndpointBaseTest(factory), IClassFixture<BackOfficeWebApplicationFactory>
{
    [Fact]
    public async Task GetMe_WithAdminIdentity_ShouldReturnIsAdminTrue()
    {
        // Arrange
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "admin");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<MeResponse>();
        payload.Should().NotBeNull();
        payload.DisplayName.Should().Be(identity.Name);
        payload.Email.Should().Be(identity.Email);
        payload.IsAdmin.Should().BeTrue();
        payload.Groups.Should().BeEquivalentTo(identity.Groups);
    }

    [Fact]
    public async Task GetMe_WithNonAdminIdentity_ShouldReturnIsAdminFalse()
    {
        // Arrange
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<MeResponse>();
        payload.Should().NotBeNull();
        payload.DisplayName.Should().Be(identity.Name);
        payload.Email.Should().Be(identity.Email);
        payload.IsAdmin.Should().BeFalse();
        payload.Groups.Should().BeEquivalentTo(identity.Groups);
    }

    [Fact]
    public async Task GetMe_WithBrowserAcceptHeader_AndMissingPrincipal_ShouldRedirectToLogin()
    {
        // Arrange
        using var client = CreateBackOfficeClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        // Act
        var response = await client.GetAsync("/api/back-office/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().StartWith(BackOfficeIdentityDefaults.LoginPath);
        response.Headers.Location.ToString().Should().Contain("post_login_redirect_uri=");
    }

    [Fact]
    public async Task GetMe_WithJsonAcceptHeader_AndMissingPrincipal_ShouldReturnUnauthorized()
    {
        // Arrange
        using var client = CreateBackOfficeClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Act
        var response = await client.GetAsync("/api/back-office/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMe_WhenCalledViaWrongHost_ShouldReturnNotFound()
    {
        // Arrange
        using var client = CreateClientForHost("app.test.localhost");
        client.DefaultRequestHeaders.Add(BackOfficeIdentityDefaults.PrincipalNameHeader, "Some User");

        // Act
        var response = await client.GetAsync("/api/back-office/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
