namespace PlatformPlatform.SharedKernel.ApplicationCore.Services;

public interface IBlobStorage
{
    Task UploadAsync(string containerName, string blobName, string contentType, Stream stream, CancellationToken cancellationToken);
    
    string GetBlobUrl(string container, string blobName);
    
    string GetSharedAccessSignature(string container, TimeSpan expiresIn);
}
