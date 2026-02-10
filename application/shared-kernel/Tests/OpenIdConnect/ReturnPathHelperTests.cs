using FluentAssertions;
using Microsoft.AspNetCore.Http;
using PlatformPlatform.SharedKernel.OpenIdConnect;
using Xunit;

namespace PlatformPlatform.SharedKernel.Tests.OpenIdConnect;

public sealed class ReturnPathHelperTests
{
    [Theory]
    [InlineData("/dashboard", "%2Fdashboard")]
    [InlineData("/users/123", "%2Fusers%2F123")]
    [InlineData("/path/to/resource", "%2Fpath%2Fto%2Fresource")]
    [InlineData("/", "%2F")]
    public void SetReturnPathCookie_WhenValidPath_ShouldSetCookie(string returnPath, string expectedEncodedPath)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();

        // Act
        ReturnPathHelper.SetReturnPathCookie(httpContext, returnPath);

        // Assert
        httpContext.Response.Headers.SetCookie.Should().NotBeEmpty();
        var cookieHeader = httpContext.Response.Headers.SetCookie.ToString();
        cookieHeader.Should().Contain(ReturnPathHelper.ReturnPathCookieName);
        cookieHeader.Should().Contain(expectedEncodedPath);
        cookieHeader.Should().Contain("max-age=300");
    }

    [Theory]
    [InlineData("dashboard")]
    [InlineData("//evil.com/path")]
    [InlineData("/path\\with\\backslash")]
    [InlineData("https://evil.com/path")]
    [InlineData("/path://somewhere")]
    [InlineData("/%2F/evil.com")]
    [InlineData("/%2f%2fevil.com")]
    [InlineData("/%2F%2Fevil.com")]
    [InlineData("/\\evil.com")]
    [InlineData("/%5Cevil.com")]
    [InlineData("/%5C%5Cevil.com")]
    [InlineData("/..%2f..%2fetc")]
    public void SetReturnPathCookie_WhenInvalidPath_ShouldNotSetCookie(string returnPath)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();

        // Act
        ReturnPathHelper.SetReturnPathCookie(httpContext, returnPath);

        // Assert
        httpContext.Response.Headers.SetCookie.Should().BeEmpty();
    }

    [Theory]
    [InlineData("/dashboard")]
    [InlineData("/users/123")]
    [InlineData("/")]
    public void GetReturnPathCookie_WhenValidCookieExists_ShouldReturnPath(string expectedPath)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Cookie = $"{ReturnPathHelper.ReturnPathCookieName}={expectedPath}";

        // Act
        var returnPath = ReturnPathHelper.GetReturnPathCookie(httpContext);

        // Assert
        returnPath.Should().Be(expectedPath);
    }

    [Fact]
    public void GetReturnPathCookie_WhenCookieDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();

        // Act
        var returnPath = ReturnPathHelper.GetReturnPathCookie(httpContext);

        // Assert
        returnPath.Should().BeNull();
    }

    [Fact]
    public void GetReturnPathCookie_WhenCookieIsEmpty_ShouldReturnNull()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Cookie = $"{ReturnPathHelper.ReturnPathCookieName}=";

        // Act
        var returnPath = ReturnPathHelper.GetReturnPathCookie(httpContext);

        // Assert
        returnPath.Should().BeNull();
    }

    [Theory]
    [InlineData("//evil.com/path")]
    [InlineData("/path\\with\\backslash")]
    [InlineData("https://evil.com/path")]
    [InlineData("/%2F/evil.com")]
    [InlineData("/%2f%2fevil.com")]
    [InlineData("/%2F%2Fevil.com")]
    [InlineData("/\\evil.com")]
    [InlineData("/%5Cevil.com")]
    [InlineData("/%5C%5Cevil.com")]
    [InlineData("/..%2f..%2fetc")]
    public void GetReturnPathCookie_WhenCookieContainsInvalidPath_ShouldReturnNull(string invalidPath)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Cookie = $"{ReturnPathHelper.ReturnPathCookieName}={invalidPath}";

        // Act
        var returnPath = ReturnPathHelper.GetReturnPathCookie(httpContext);

        // Assert
        returnPath.Should().BeNull();
    }

    [Fact]
    public void ClearReturnPathCookie_WhenCalled_ShouldDeleteCookie()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();

        // Act
        ReturnPathHelper.ClearReturnPathCookie(httpContext);

        // Assert
        httpContext.Response.Headers.SetCookie.Should().NotBeEmpty();
        httpContext.Response.Headers.SetCookie.ToString().Should().Contain(ReturnPathHelper.ReturnPathCookieName);
        httpContext.Response.Headers.SetCookie.ToString().Should().Contain("expires=");
    }
}
