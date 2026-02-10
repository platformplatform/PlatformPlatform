using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.AccountManagement.Integrations.OAuth;
using PlatformPlatform.AccountManagement.Integrations.OAuth.Mock;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.ExternalAuthentication;

public sealed class MockOAuthProviderEnforcementTests
{
    [Fact]
    public void MockEmail_ShouldEndWithMockLocalhostDomain()
    {
        // Assert
        MockOAuthProvider.MockEmail.Should().EndWith(OAuthProviderFactory.MockEmailDomain);
    }

    [Fact]
    public void ShouldUseMockProvider_WhenMockProviderDisabled_ShouldReturnFalse()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["OAuth:AllowMockProvider"] = "false" })
            .Build();
        var factory = new OAuthProviderFactory(new ServiceCollection().BuildServiceProvider(), configuration);
        var httpContext = new DefaultHttpContext();

        // Act
        var result = factory.ShouldUseMockProvider(httpContext);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldUseMockProvider_WhenMockProviderEnabledButNoCookie_ShouldReturnFalse()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["OAuth:AllowMockProvider"] = "true" })
            .Build();
        var factory = new OAuthProviderFactory(new ServiceCollection().BuildServiceProvider(), configuration);
        var httpContext = new DefaultHttpContext();

        // Act
        var result = factory.ShouldUseMockProvider(httpContext);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldUseMockProvider_WhenMockProviderEnabledWithCookie_ShouldReturnTrue()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["OAuth:AllowMockProvider"] = "true" })
            .Build();
        var factory = new OAuthProviderFactory(new ServiceCollection().BuildServiceProvider(), configuration);
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Append("Cookie", $"{OAuthProviderFactory.UseMockProviderCookieName}=true");

        // Act
        var result = factory.ShouldUseMockProvider(httpContext);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task GetUserProfileAsync_ShouldAlwaysReturnMockLocalhostEmail()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["OAuth:AllowMockProvider"] = "true" })
            .Build();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Append("Cookie", $"{OAuthProviderFactory.UseMockProviderCookieName}=true");
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var mockProvider = new MockOAuthProvider(configuration, httpContextAccessor);
        var tokenResponse = new OAuthTokenResponse("mock-access-token", "mock-id-token:test-nonce", 3600);

        // Act
        var profile = await mockProvider.GetUserProfileAsync(tokenResponse, CancellationToken.None);

        // Assert
        profile.Should().NotBeNull();
        profile.Email.Should().EndWith(OAuthProviderFactory.MockEmailDomain);
    }

    [Fact]
    public async Task GetUserProfileAsync_WhenCustomEmailPrefix_ShouldReturnMockLocalhostEmail()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["OAuth:AllowMockProvider"] = "true" })
            .Build();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Append("Cookie", $"{OAuthProviderFactory.UseMockProviderCookieName}=customuser");
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var mockProvider = new MockOAuthProvider(configuration, httpContextAccessor);
        var tokenResponse = new OAuthTokenResponse("mock-access-token", "mock-id-token:test-nonce", 3600);

        // Act
        var profile = await mockProvider.GetUserProfileAsync(tokenResponse, CancellationToken.None);

        // Assert
        profile.Should().NotBeNull();
        profile.Email.Should().Be($"customuser{OAuthProviderFactory.MockEmailDomain}");
    }
}
