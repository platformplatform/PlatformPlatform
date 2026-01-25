using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using NSubstitute;
using PlatformPlatform.Account.Integrations.OAuth;
using Xunit;

namespace PlatformPlatform.Account.Tests.ExternalAuthentication;

public sealed class ExternalAvatarClientTests
{
    [Theory]
    [InlineData("https://lh3.googleusercontent.com/a/photo123")]
    [InlineData("https://lh4.googleusercontent.com/a/photo456")]
    [InlineData("https://lh6.googleusercontent.com/a/photo789")]
    [InlineData("https://www.gravatar.com/avatar/abc123")]
    public async Task DownloadAvatarAsync_WhenDomainIsAllowlisted_ShouldAttemptDownload(string avatarUrl)
    {
        // Arrange
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47])
                {
                    Headers = { ContentType = new MediaTypeHeaderValue("image/png") }
                }
            }
        );
        var httpClient = new HttpClient(handler);
        var logger = Substitute.For<ILogger<ExternalAvatarClient>>();
        var client = new ExternalAvatarClient(httpClient, logger);

        // Act
        var result = await client.DownloadAvatarAsync(avatarUrl, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ContentType.Should().Be("image/png");
        handler.WasRequestSent.Should().BeTrue();
    }

    [Theory]
    [InlineData("https://evil.com/avatar.png")]
    [InlineData("https://notgoogleusercontent.com/a/photo")]
    [InlineData("https://fakegravatar.com/avatar/abc")]
    [InlineData("https://googleusercontent.com.evil.com/photo")]
    [InlineData("https://169.254.169.254/metadata")]
    [InlineData("https://internal-service.local/secret")]
    public async Task DownloadAvatarAsync_WhenDomainIsNotAllowlisted_ShouldReturnNull(string avatarUrl)
    {
        // Arrange
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var httpClient = new HttpClient(handler);
        var logger = Substitute.For<ILogger<ExternalAvatarClient>>();
        var client = new ExternalAvatarClient(httpClient, logger);

        // Act
        var result = await client.DownloadAvatarAsync(avatarUrl, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        handler.WasRequestSent.Should().BeFalse();
    }

    [Theory]
    [InlineData("http://lh3.googleusercontent.com/a/photo")]
    [InlineData("ftp://lh3.googleusercontent.com/a/photo")]
    public async Task DownloadAvatarAsync_WhenSchemeIsNotHttps_ShouldReturnNull(string avatarUrl)
    {
        // Arrange
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var httpClient = new HttpClient(handler);
        var logger = Substitute.For<ILogger<ExternalAvatarClient>>();
        var client = new ExternalAvatarClient(httpClient, logger);

        // Act
        var result = await client.DownloadAvatarAsync(avatarUrl, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        handler.WasRequestSent.Should().BeFalse();
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("")]
    [InlineData("/relative/path")]
    public async Task DownloadAvatarAsync_WhenUrlIsMalformed_ShouldReturnNull(string avatarUrl)
    {
        // Arrange
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var httpClient = new HttpClient(handler);
        var logger = Substitute.For<ILogger<ExternalAvatarClient>>();
        var client = new ExternalAvatarClient(httpClient, logger);

        // Act
        var result = await client.DownloadAvatarAsync(avatarUrl, CancellationToken.None);

        // Assert
        result.Should().BeNull();
        handler.WasRequestSent.Should().BeFalse();
    }

    [Fact]
    public async Task DownloadAvatarAsync_WhenContentLengthExceedsLimit_ShouldReturnNull()
    {
        // Arrange
        var content = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47]);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Headers.ContentLength = 2 * 1024 * 1024;
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        var httpClient = new HttpClient(handler);
        var logger = Substitute.For<ILogger<ExternalAvatarClient>>();
        var client = new ExternalAvatarClient(httpClient, logger);

        // Act
        var result = await client.DownloadAvatarAsync("https://lh3.googleusercontent.com/a/photo", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DownloadAvatarAsync_WhenBodyExceedsLimitWithoutContentLength_ShouldReturnNull()
    {
        // Arrange
        var oversizedBody = new byte[2 * 1024 * 1024];
        var content = new ByteArrayContent(oversizedBody);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Headers.ContentLength = null;
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        var httpClient = new HttpClient(handler);
        var logger = Substitute.For<ILogger<ExternalAvatarClient>>();
        var client = new ExternalAvatarClient(httpClient, logger);

        // Act
        var result = await client.DownloadAvatarAsync("https://lh3.googleusercontent.com/a/photo", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DownloadAvatarAsync_WhenBodyIsWithinLimit_ShouldReturnAvatar()
    {
        // Arrange
        var imageData = new byte[512 * 1024];
        var content = new ByteArrayContent(imageData);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        content.Headers.ContentLength = null;
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        var httpClient = new HttpClient(handler);
        var logger = Substitute.For<ILogger<ExternalAvatarClient>>();
        var client = new ExternalAvatarClient(httpClient, logger);

        // Act
        var result = await client.DownloadAvatarAsync("https://lh3.googleusercontent.com/a/photo", CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ContentType.Should().Be("image/jpeg");
        result.Stream.Length.Should().Be(512 * 1024);
    }

    private sealed class MockHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public bool WasRequestSent { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            WasRequestSent = true;
            return Task.FromResult(response);
        }
    }
}
