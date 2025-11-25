using Azure.Storage.Blobs.Models;

namespace PlatformPlatform.SharedKernel.Integrations.BlobStorage;

public interface IBlobStorageClient
{
    Task UploadAsync(string containerName, string blobName, string contentType, Stream stream, CancellationToken cancellationToken);

    Task<(Stream Stream, string ContentType)?> DownloadAsync(string containerName, string blobName, CancellationToken cancellationToken);

    string GetBlobUrl(string container, string blobName);

    string GetSharedAccessSignature(string container, TimeSpan expiresIn);

    Task CreateContainerIfNotExistsAsync(string containerName, PublicAccessType publicAccessType, CancellationToken cancellationToken);
}
