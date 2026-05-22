using Account.Features.SupportTickets.Domain;

namespace Account.Features.SupportTickets.Shared;

// Stable virtual paths the UI uses as attachment hyperlinks. The matching endpoints in
// Account.Api stream the blob with auth and tenant isolation enforced. The {fileName} segment is
// the blob name suffix the uploader produced (hash + sanitized file name).
public static class SupportTicketAttachmentDownloader
{
    public static string BuildReporterDownloadUrl(SupportTicketId ticketId, SupportMessageId messageId, string blobUrl)
    {
        return $"/api/account/support-tickets/{ticketId}/messages/{messageId}/attachments/{ExtractFileName(blobUrl)}";
    }

    public static string BuildStaffDownloadUrl(SupportTicketId ticketId, SupportMessageId messageId, string blobUrl)
    {
        return $"/api/back-office/support-tickets/{ticketId}/messages/{messageId}/attachments/{ExtractFileName(blobUrl)}";
    }

    // Parses a stored BlobUrl into its container and blob name, returning null when the URL is
    // malformed or its container is not one of the two known support containers. The managed
    // identity can read every container in the storage account, so the container name read out of a
    // stored value must be validated against the allow-list rather than trusted blindly.
    public static (string ContainerName, string BlobName)? TryParseBlobUrl(string blobUrl)
    {
        var trimmed = blobUrl.TrimStart('/');
        var firstSlash = trimmed.IndexOf('/');
        if (firstSlash <= 0 || firstSlash == trimmed.Length - 1) return null;

        var containerName = trimmed[..firstSlash];
        if (containerName != SupportAttachmentUploader.TenantContainerName && containerName != SupportAttachmentUploader.StaffContainerName)
        {
            return null;
        }

        return (containerName, trimmed[(firstSlash + 1)..]);
    }

    public static string ExtractFileName(string blobUrl)
    {
        var lastSlash = blobUrl.LastIndexOf('/');
        return lastSlash >= 0 ? blobUrl[(lastSlash + 1)..] : blobUrl;
    }
}
