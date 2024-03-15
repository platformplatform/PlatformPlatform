using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using PlatformPlatform.SharedKernel.ApplicationCore.Services;

namespace PlatformPlatform.SharedKernel.InfrastructureCore.Services;

public class BlobStorage(BlobServiceClient blobServiceClient) : IBlobStorage
{
    public async Task UploadAsync(
        string containerName,
        string blobName,
        string contentType,
        Stream stream,
        CancellationToken cancellationToken
    )
    {
        var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = blobContainerClient.GetBlobClient(blobName);
        var blobHttpHeaders = new BlobHttpHeaders { ContentType = contentType };
        await blobClient.UploadAsync(stream, blobHttpHeaders, cancellationToken: cancellationToken);
    }

    public string GetBlobUrl(string container, string blobName)
    {
        return $"{blobServiceClient.Uri}/{container}/{blobName}";
    }
}