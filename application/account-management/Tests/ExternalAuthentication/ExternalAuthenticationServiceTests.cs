using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PlatformPlatform.AccountManagement.Features.ExternalAuthentication;
using PlatformPlatform.AccountManagement.Features.ExternalAuthentication.Domain;
using PlatformPlatform.AccountManagement.Integrations.OAuth;
using Xunit;

namespace PlatformPlatform.AccountManagement.Tests.ExternalAuthentication;

public sealed class ExternalAuthenticationServiceTests
{
    private static ExternalAuthenticationService CreateService(HttpContext httpContext, bool allowMockProvider = false)
    {
        return CreateServiceWithProvider(httpContext, new EphemeralDataProtectionProvider(), allowMockProvider);
    }

    private static ExternalAuthenticationService CreateServiceWithProvider(HttpContext httpContext, IDataProtectionProvider dataProtectionProvider, bool allowMockProvider = false)
    {
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["OAuth:AllowMockProvider"] = allowMockProvider.ToString().ToLowerInvariant()
                }
            )
            .Build();
        var oauthProviderFactory = new OAuthProviderFactory(new ServiceCollection().BuildServiceProvider(), configuration);
        var logger = NullLogger<ExternalAuthenticationService>.Instance;

        return new ExternalAuthenticationService(httpContextAccessor, dataProtectionProvider, oauthProviderFactory, logger);
    }

    [Fact]
    public void GenerateBrowserFingerprintHash_ShouldReturnSha256OfUserAgentAndAcceptLanguage()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["User-Agent"] = "TestBrowser/1.0";
        httpContext.Request.Headers["Accept-Language"] = "en-US";
        var service = CreateService(httpContext);

        var expectedFingerprint = "TestBrowser/1.0|en-US";
        var expectedHash = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(expectedFingerprint)));

        // Act
        var hash = service.GenerateBrowserFingerprintHash();

        // Assert
        hash.Should().Be(expectedHash);
    }

    [Fact]
    public void GenerateBrowserFingerprintHash_WhenDifferentHeaders_ShouldReturnDifferentHash()
    {
        // Arrange
        var httpContext1 = new DefaultHttpContext();
        httpContext1.Request.Headers["User-Agent"] = "Chrome/120";
        httpContext1.Request.Headers["Accept-Language"] = "en-US";
        var service1 = CreateService(httpContext1);

        var httpContext2 = new DefaultHttpContext();
        httpContext2.Request.Headers["User-Agent"] = "Firefox/121";
        httpContext2.Request.Headers["Accept-Language"] = "da-DK";
        var service2 = CreateService(httpContext2);

        // Act
        var hash1 = service1.GenerateBrowserFingerprintHash();
        var hash2 = service2.GenerateBrowserFingerprintHash();

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void ValidateBrowserFingerprint_WhenMatchingFingerprint_ShouldReturnTrue()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["User-Agent"] = "TestBrowser/1.0";
        httpContext.Request.Headers["Accept-Language"] = "en-US";
        var service = CreateService(httpContext);
        var fingerprintHash = service.GenerateBrowserFingerprintHash();

        // Act
        var result = service.ValidateBrowserFingerprint(fingerprintHash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateBrowserFingerprint_WhenMismatchedFingerprint_ShouldReturnFalse()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["User-Agent"] = "TestBrowser/1.0";
        httpContext.Request.Headers["Accept-Language"] = "en-US";
        var service = CreateService(httpContext);

        var differentFingerprint = Convert.ToBase64String(SHA256.HashData("DifferentBrowser/2.0|da-DK"u8));

        // Act
        var result = service.ValidateBrowserFingerprint(differentFingerprint);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateBrowserFingerprint_WhenMockProvider_ShouldAlwaysReturnTrue()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["User-Agent"] = "TestBrowser/1.0";
        httpContext.Request.Headers["Accept-Language"] = "en-US";
        httpContext.Request.Headers.Append("Cookie", $"{OAuthProviderFactory.UseMockProviderCookieName}=true");
        var service = CreateService(httpContext, true);

        var wrongFingerprint = Convert.ToBase64String(SHA256.HashData("CompletelyWrong"u8));

        // Act
        var result = service.ValidateBrowserFingerprint(wrongFingerprint);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void GetExternalLoginIdFromState_WhenValidState_ShouldReturnExternalLoginId()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var service = CreateService(httpContext);
        var externalLoginId = new ExternalLoginId("extlg_01TESTID12345678901234");
        var protectedState = service.ProtectState(externalLoginId);

        // Act
        var result = service.GetExternalLoginIdFromState(protectedState);

        // Assert
        result.Should().NotBeNull();
        result.ToString().Should().Be(externalLoginId.ToString());
    }

    [Fact]
    public void GetExternalLoginIdFromState_WhenTamperedState_ShouldReturnNull()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var service = CreateService(httpContext);

        // Act
        var result = service.GetExternalLoginIdFromState("garbage-tampered-data");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetExternalLoginIdFromState_WhenNullState_ShouldReturnNull()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var service = CreateService(httpContext);

        // Act
        var result = service.GetExternalLoginIdFromState(null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetExternalLoginIdFromState_WhenEmptyState_ShouldReturnNull()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var service = CreateService(httpContext);

        // Act
        var result = service.GetExternalLoginIdFromState("");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetExternalLoginCookie_WhenTamperedCookie_ShouldReturnNull()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Append("Cookie", "__Host-external-login=corrupted-encrypted-data");
        var service = CreateService(httpContext);

        // Act
        var result = service.GetExternalLoginCookie();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetExternalLoginCookie_WhenMissingCookie_ShouldReturnNull()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var service = CreateService(httpContext);

        // Act
        var result = service.GetExternalLoginCookie();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetExternalLoginCookie_WhenValidCookieWithoutPreferredTenant_ShouldReturnCookie()
    {
        // Arrange
        var dataProtectionProvider = new EphemeralDataProtectionProvider();
        var externalLoginId = ExternalLoginId.NewId();

        var writeContext = new DefaultHttpContext();
        writeContext.Request.Headers["User-Agent"] = "TestBrowser/1.0";
        writeContext.Request.Headers["Accept-Language"] = "en-US";
        var writeService = CreateServiceWithProvider(writeContext, dataProtectionProvider);
        writeService.SetExternalLoginCookie(externalLoginId);

        var setCookieHeader = writeContext.Response.Headers["Set-Cookie"].ToString();
        var cookieValue = setCookieHeader.Split(';')[0].Split('=', 2)[1];

        var readContext = new DefaultHttpContext();
        readContext.Request.Headers.Append("Cookie", $"__Host-external-login={cookieValue}");
        var readService = CreateServiceWithProvider(readContext, dataProtectionProvider);

        // Act
        var result = readService.GetExternalLoginCookie();

        // Assert
        result.Should().NotBeNull();
        result.ExternalLoginId.Should().Be(externalLoginId);
    }

    [Fact]
    public void GetExternalLoginCookie_WhenCookieHasWrongNumberOfParts_ShouldReturnNull()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var dataProtectionProvider = new EphemeralDataProtectionProvider();
        var dataProtector = dataProtectionProvider.CreateProtector("ExternalLogin");
        var malformedValue = dataProtector.Protect("only-one-part");
        httpContext.Request.Headers.Append("Cookie", $"__Host-external-login={malformedValue}");
        var service = CreateServiceWithProvider(httpContext, dataProtectionProvider);

        // Act
        var result = service.GetExternalLoginCookie();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetExternalLoginCookie_WhenCookieHasInvalidId_ShouldReturnNull()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var dataProtectionProvider = new EphemeralDataProtectionProvider();
        var dataProtector = dataProtectionProvider.CreateProtector("ExternalLogin");
        var malformedValue = dataProtector.Protect("not-a-valid-id|some-fingerprint");
        httpContext.Request.Headers.Append("Cookie", $"__Host-external-login={malformedValue}");
        var service = CreateServiceWithProvider(httpContext, dataProtectionProvider);

        // Act
        var result = service.GetExternalLoginCookie();

        // Assert
        result.Should().BeNull();
    }
}
