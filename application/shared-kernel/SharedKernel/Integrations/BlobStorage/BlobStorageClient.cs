using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace PlatformPlatform.SharedKernel.Integrations.BlobStorage;

public class BlobStorageClient(BlobServiceClient blobServiceClient, TimeProvider timeProvider) : IBlobStorageClient
{
    public async Task UploadAsync(string containerName, string blobName, string contentType, Stream stream, CancellationToken cancellationToken)
    {
        var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = blobContainerClient.GetBlobClient(blobName);
        var blobHttpHeaders = new BlobHttpHeaders { ContentType = contentType };
        await blobClient.UploadAsync(stream, blobHttpHeaders, cancellationToken: cancellationToken);
    }

    public async Task<(Stream Stream, string ContentType)?> DownloadAsync(string containerName, string blobName, CancellationToken cancellationToken)
    {
        var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = blobContainerClient.GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync(cancellationToken))
        {
            return null;
        }

        var response = await blobClient.DownloadStreamingAsync(cancellationToken: cancellationToken);
        return (response.Value.Content, response.Value.Details.ContentType);
    }

    public string GetBlobUrl(string container, string blobName)
    {
        return $"{blobServiceClient.Uri}/{container}/{blobName}";
    }

    public async Task<bool> DeleteIfExistsAsync(string containerName, string blobName, CancellationToken cancellationToken)
    {
        var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = blobContainerClient.GetBlobClient(blobName);
        var response = await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        return response.Value;
    }

    public string GetSharedAccessSignature(string container, TimeSpan expiresIn)
    {
        var blobContainerClient = blobServiceClient.GetBlobContainerClient(container);
        var expiresOn = timeProvider.GetUtcNow().Add(expiresIn);
        return blobContainerClient.GenerateSasUri(BlobContainerSasPermissions.Read, expiresOn).Query;
    }

    public Uri GetBlobUriWithSharedAccessSignature(string container, string blobName, TimeSpan expiresIn)
    {
        var blobContainerClient = blobServiceClient.GetBlobContainerClient(container);
        var blobClient = blobContainerClient.GetBlobClient(blobName);
        var utcNow = timeProvider.GetUtcNow();
        var expiresOn = utcNow.Add(expiresIn);

        if (blobClient.CanGenerateSasUri)
        {
            return blobClient.GenerateSasUri(BlobSasPermissions.Read, expiresOn);
        }

        var userDelegationKey = blobServiceClient.GetUserDelegationKey(utcNow, expiresOn);
        var builder = new BlobSasBuilder
        {
            BlobContainerName = container,
            BlobName = blobName,
            Resource = "b",
            ExpiresOn = expiresOn
        };

        builder.SetPermissions(BlobSasPermissions.Read);

        return new BlobUriBuilder(blobClient.Uri)
        {
            Sas = builder.ToSasQueryParameters(userDelegationKey, blobServiceClient.AccountName)
        }.ToUri();
    }

    public async Task CreateContainerIfNotExistsAsync(string containerName, PublicAccessType publicAccessType, CancellationToken cancellationToken)
    {
        var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
        await blobContainerClient.CreateIfNotExistsAsync(publicAccessType, cancellationToken: cancellationToken);
    }
}
