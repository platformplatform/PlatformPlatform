using System.Collections.Immutable;
using System.Security.Cryptography;
using Account.Features.SupportTickets.Domain;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SharedKernel.Cqrs;
using SharedKernel.Domain;
using SharedKernel.Integrations.BlobStorage;

namespace Account.Features.SupportTickets.Shared;

public sealed class SupportAttachmentUploader([FromKeyedServices("account-storage")] IBlobStorageClient blobStorageClient)
{
    public const long MaxFileSizeBytes = 25L * 1024 * 1024;
    public const int MaxFilesPerMessage = 5;
    public const string TenantContainerName = "support-tickets";
    public const string StaffContainerName = "support-staff";

    private static readonly string[] AllowedContentTypes =
    [
        "image/jpeg",
        "image/jpg",
        "image/png",
        "image/gif",
        "image/webp",
        "application/pdf",
        "text/plain",
        "text/csv",
        "application/zip",
        "application/x-zip-compressed"
    ];

    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf", ".txt", ".csv", ".log", ".zip"];

    public Result Validate(IReadOnlyList<IFormFile> files)
    {
        if (files.Count > MaxFilesPerMessage)
        {
            return Result.BadRequest($"Up to {MaxFilesPerMessage} attachments are allowed per message.");
        }

        foreach (var file in files)
        {
            if (file.Length <= 0)
            {
                return Result.BadRequest($"Attachment '{file.FileName}' is empty.");
            }

            if (file.Length > MaxFileSizeBytes)
            {
                return Result.BadRequest($"Attachment '{file.FileName}' exceeds the 25 MB maximum size.");
            }

            if (!IsAllowedExtension(file.FileName) || !IsAllowedContentType(file.ContentType))
            {
                return Result.BadRequest($"Attachment '{file.FileName}' has a disallowed file type.");
            }
        }

        return Result.Success();
    }

    public async Task<Result<ImmutableArray<SupportMessageAttachment>>> UploadTenantAttachmentsAsync(
        TenantId tenantId,
        IReadOnlyList<IFormFile> files,
        CancellationToken cancellationToken
    )
    {
        var validation = Validate(files);
        if (!validation.IsSuccess) return Result<ImmutableArray<SupportMessageAttachment>>.From(validation);

        var builder = ImmutableArray.CreateBuilder<SupportMessageAttachment>(files.Count);
        foreach (var file in files)
        {
            builder.Add(await UploadAsync(TenantContainerName, $"{tenantId}", file, cancellationToken));
        }

        return builder.ToImmutable();
    }

    public async Task<Result<ImmutableArray<SupportMessageAttachment>>> UploadStaffAttachmentsAsync(
        IReadOnlyList<IFormFile> files,
        CancellationToken cancellationToken
    )
    {
        var validation = Validate(files);
        if (!validation.IsSuccess) return Result<ImmutableArray<SupportMessageAttachment>>.From(validation);

        var builder = ImmutableArray.CreateBuilder<SupportMessageAttachment>(files.Count);
        foreach (var file in files)
        {
            builder.Add(await UploadAsync(StaffContainerName, "messages", file, cancellationToken));
        }

        return builder.ToImmutable();
    }

    private async Task<SupportMessageAttachment> UploadAsync(string containerName, string blobPrefix, IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var hash = await ComputeStreamHash(stream, cancellationToken);
        var safeFileName = SanitizeFileName(file.FileName);
        var blobName = $"{blobPrefix}/{hash}-{safeFileName}";

        await blobStorageClient.UploadAsync(containerName, blobName, file.ContentType, stream, cancellationToken);

        var blobUrl = $"/{containerName}/{blobName}";
        return new SupportMessageAttachment(file.FileName, file.ContentType, file.Length, blobUrl);
    }

    private static bool IsAllowedExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return !string.IsNullOrEmpty(extension) && AllowedExtensions.Contains(extension.ToLowerInvariant());
    }

    private static bool IsAllowedContentType(string contentType)
    {
        return !string.IsNullOrEmpty(contentType) && AllowedContentTypes.Contains(contentType.ToLowerInvariant());
    }

    [UsedImplicitly]
    private static string SanitizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        var sanitized = new string(name.Select(c => char.IsLetterOrDigit(c) || c is '.' or '-' or '_' ? c : '_').ToArray());
        return sanitized.Length > 80 ? sanitized[..80] : sanitized;
    }

    private static async Task<string> ComputeStreamHash(Stream stream, CancellationToken cancellationToken)
    {
        using var sha1 = SHA1.Create();
        var hashBytes = await sha1.ComputeHashAsync(stream, cancellationToken);
        stream.Position = 0;
        return BitConverter.ToString(hashBytes).Replace("-", "")[..16].ToUpperInvariant();
    }
}
