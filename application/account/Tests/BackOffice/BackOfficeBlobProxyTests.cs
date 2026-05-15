using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using SharedKernel.Integrations.BlobStorage;
using Xunit;

namespace Account.Tests.BackOffice;

public sealed class BackOfficeBlobProxyTests(BackOfficeBlobProxyFactory factory)
    : BackOfficeEndpointBaseTest(factory), IClassFixture<BackOfficeBlobProxyFactory>
{
    private readonly IBlobStorageClient _blobStorageClient = factory.BlobStorageClient;

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
}

public sealed class BackOfficeBlobProxyFactory : BackOfficeWebApplicationFactory
{
    // Shared across every test in the class (IClassFixture lifetime). If more tests are added,
    // call ClearSubstitute() / ClearReceivedCalls() at the top of each so configured behaviours
    // and ReceivedCalls() do not leak between tests.
    public IBlobStorageClient BlobStorageClient { get; } = Substitute.For<IBlobStorageClient>();

    protected override void ConfigureAdditionalTestServices(IServiceCollection services)
    {
        services.RemoveAll(typeof(IBlobStorageClient));
        services.AddKeyedSingleton("account-storage", BlobStorageClient);
    }
}
