using Account.Features.SupportTickets.Shared;
using FluentAssertions;
using Xunit;

namespace Account.Tests.SupportTickets;

public sealed class SupportTicketAttachmentDownloaderTests
{
    [Theory]
    [InlineData("/support-tickets/1/HASH-file.png", "support-tickets", "1/HASH-file.png")]
    [InlineData("support-tickets/1/HASH-file.png", "support-tickets", "1/HASH-file.png")]
    [InlineData("/support-staff/messages/HASH-note.pdf", "support-staff", "messages/HASH-note.pdf")]
    public void TryParseBlobUrl_WhenContainerIsAllowed_ShouldReturnContainerAndBlobName(string blobUrl, string expectedContainer, string expectedBlobName)
    {
        var parsed = SupportTicketAttachmentDownloader.TryParseBlobUrl(blobUrl);

        parsed.Should().NotBeNull();
        parsed.Value.ContainerName.Should().Be(expectedContainer);
        parsed.Value.BlobName.Should().Be(expectedBlobName);
    }

    [Theory]
    [InlineData("/avatars/1/me.png")] // foreign container the managed identity can also read
    [InlineData("/logos/1/logo.png")]
    [InlineData("/unknown/blob.bin")]
    [InlineData("")] // empty
    [InlineData("no-slash")] // no separator
    [InlineData("/support-tickets")] // leading slash only, no blob name
    [InlineData("/support-tickets/")] // trailing slash, empty blob name
    public void TryParseBlobUrl_WhenContainerNotAllowedOrMalformed_ShouldReturnNull(string blobUrl)
    {
        SupportTicketAttachmentDownloader.TryParseBlobUrl(blobUrl).Should().BeNull();
    }
}
