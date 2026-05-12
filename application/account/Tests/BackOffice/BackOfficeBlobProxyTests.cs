using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using SharedKernel.Integrations.BlobStorage;
using Xunit;

namespace Account.Tests.BackOffice;

public sealed class BackOfficeBlobProxyTests : BackOfficeEndpointBaseTest
{
    private readonly IBlobStorageClient _blobStorageClient = Substitute.For<IBlobStorageClient>();

    [Fact]
    public async Task BackOfficeBlobProxy_WhenServingBlob_ShouldSetNoSniffHeader()
    {
        // Arrange
        var blobBytes = "fake-image-bytes"u8.ToArray();
        _blobStorageClient
            .DownloadAsync("logos", "tenant/logo/HASH.png", Arg.Any<CancellationToken>())
            .Returns((new MemoryStream(blobBytes), "image/png"));

        using var client = CreateBackOfficeClient();

        // Act
        var response = await client.GetAsync("/logos/tenant/logo/HASH.png");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");
    }

    protected override void ConfigureAdditionalTestServices(IServiceCollection services)
    {
        services.RemoveAll(typeof(IBlobStorageClient));
        services.AddKeyedSingleton("account-storage", _blobStorageClient);
    }
}
