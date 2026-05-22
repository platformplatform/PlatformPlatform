using Account.Features.SupportTickets.Domain;
using Account.Features.SupportTickets.Shared;
using SharedKernel.ExecutionContext;
using SharedKernel.Integrations.BlobStorage;

namespace Account.Api;

// Streams a support-ticket attachment blob with auth gating. The handler reads the ticket through
// the repository so tenant isolation, soft-delete filters, and the reporter-only check apply
// before any blob bytes leave storage.
internal static class SupportTicketAttachmentEndpoint
{
    public static async Task<IResult> DownloadForReporterAsync(
        SupportTicketId ticketId,
        SupportMessageId messageId,
        string fileName,
        ISupportTicketRepository ticketRepository,
        IExecutionContext executionContext,
        IBlobStorageClient blobStorageClient,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var ticket = await ticketRepository.GetByIdAsync(ticketId, cancellationToken);
        if (ticket is null || ticket.ReporterId != executionContext.UserInfo.Id!) return Results.NotFound();

        // Reporters must never receive internal-note attachments. The detail query already hides
        // internal messages; mirror that invariant here so a leaked internal SupportMessageId can
        // not be turned into bytes via this endpoint.
        var message = ticket.Messages.SingleOrDefault(m => m.Id == messageId && m.AuthorKind != SupportMessageAuthorKind.Internal);
        return await StreamMessageAttachmentAsync(message, fileName, SupportAttachmentUploader.TenantContainerName, blobStorageClient, httpContext, cancellationToken);
    }

    public static async Task<IResult> DownloadForStaffAsync(
        SupportTicketId ticketId,
        SupportMessageId messageId,
        string fileName,
        ISupportTicketRepository ticketRepository,
        IBlobStorageClient blobStorageClient,
        HttpContext httpContext,
        CancellationToken cancellationToken
    )
    {
        var ticket = await ticketRepository.GetByIdUnfilteredAsync(ticketId, cancellationToken);
        if (ticket is null) return Results.NotFound();

        var message = ticket.Messages.SingleOrDefault(m => m.Id == messageId);
        // Staff legitimately read both customer-attached and staff-only (internal-note) attachments,
        // so no container is pinned for this path.
        return await StreamMessageAttachmentAsync(message, fileName, null, blobStorageClient, httpContext, cancellationToken);
    }

    private static async Task<IResult> StreamMessageAttachmentAsync(SupportMessage? message, string fileName, string? expectedContainerName, IBlobStorageClient blobStorageClient, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var attachment = message?.Attachments.SingleOrDefault(a => SupportTicketAttachmentDownloader.ExtractFileName(a.BlobUrl).Equals(fileName, StringComparison.Ordinal));
        if (attachment is null) return Results.NotFound();

        var parsed = SupportTicketAttachmentDownloader.TryParseBlobUrl(attachment.BlobUrl);
        if (parsed is null) return Results.NotFound();

        // Pin the container per actor: a reporter download must only ever read from the tenant
        // container, never the staff-only one, regardless of what the stored BlobUrl claims.
        if (expectedContainerName is not null && parsed.Value.ContainerName != expectedContainerName) return Results.NotFound();

        var blob = await blobStorageClient.DownloadAsync(parsed.Value.ContainerName, parsed.Value.BlobName, cancellationToken);
        if (blob is null) return Results.NotFound();

        httpContext.Response.Headers.CacheControl = "private, no-store";
        httpContext.Response.Headers.XContentTypeOptions = "nosniff";
        // Force a download (rather than inline preview) so a script-bearing file type is never
        // executed in the user's browser session against the application's origin.
        httpContext.Response.Headers.ContentDisposition = $"attachment; filename=\"{SanitizeForContentDisposition(attachment.FileName)}\"";
        return Results.Stream(blob.Value.Stream, attachment.ContentType);
    }

    private static string SanitizeForContentDisposition(string fileName)
    {
        // RFC 6266 quoted-string disallows backslashes and double quotes.
        return fileName.Replace("\\", "_").Replace("\"", "_");
    }
}
