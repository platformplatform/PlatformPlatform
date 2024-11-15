using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Integrations.BlobStorage;

namespace PlatformPlatform.AccountManagement.Features.Users.Shared;

public sealed class AvatarUpdater(IUserRepository userRepository, [FromKeyedServices("avatars-storage")] BlobStorageClient blobStorageClient)
{
    private const string ContainerName = "avatars";

    public async Task<bool> UpdateAvatar(User user, bool isGravatar, string contentType, Stream stream, CancellationToken cancellationToken)
    {
        var fileHash = await GetFileHash(stream, cancellationToken);
        var fileExtension = contentType.Split('/')[1];
        var blobName = $"{user.TenantId}/{user.Id}/{fileHash}.{fileExtension}";
        var avatarUrl = $"/{ContainerName}/{blobName}";

        if (user.Avatar.Url == avatarUrl && user.Avatar.IsGravatar == isGravatar)
        {
            return false;
        }

        await blobStorageClient.UploadAsync(ContainerName, blobName, contentType, stream, cancellationToken);

        user.UpdateAvatar(avatarUrl, isGravatar);
        userRepository.Update(user);

        return true;
    }

    private static async Task<string> GetFileHash(Stream fileStream, CancellationToken cancellationToken)
    {
        using var sha1 = SHA1.Create();
        var hashBytes = await sha1.ComputeHashAsync(fileStream, cancellationToken);
        fileStream.Position = 0;
        // This just needs to be unique for one user, who likely will ever only have one avatar, so 16 chars should be enough
        return BitConverter.ToString(hashBytes).Replace("-", "")[..16].ToUpper();
    }
}
