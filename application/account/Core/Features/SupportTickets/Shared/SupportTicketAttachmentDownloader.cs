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

    public static (string ContainerName, string BlobName) ParseBlobUrl(string blobUrl)
    {
        var trimmed = blobUrl.TrimStart('/');
        var firstSlash = trimmed.IndexOf('/');
        return (trimmed[..firstSlash], trimmed[(firstSlash + 1)..]);
    }

    public static string ExtractFileName(string blobUrl)
    {
        var lastSlash = blobUrl.LastIndexOf('/');
        return lastSlash >= 0 ? blobUrl[(lastSlash + 1)..] : blobUrl;
    }
}
