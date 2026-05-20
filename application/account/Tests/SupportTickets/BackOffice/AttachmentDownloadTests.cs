using System.Net;
using System.Text.Json;
using Account.Features.SupportTickets.Domain;
using Account.Features.Users.Domain;
using Account.Tests.BackOffice;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using NSubstitute.ClearExtensions;
using SharedKernel.Authentication.MockEasyAuth;
using SharedKernel.Domain;
using SharedKernel.Integrations.BlobStorage;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.SupportTickets.BackOffice;

public sealed class BackOfficeAttachmentDownloadFactory : BackOfficeWebApplicationFactory
{
    public IBlobStorageClient BlobStorageClient { get; } = Substitute.For<IBlobStorageClient>();

    protected override void ConfigureAdditionalTestServices(IServiceCollection services)
    {
        services.RemoveAll(typeof(IBlobStorageClient));
        services.AddKeyedSingleton("account-storage", BlobStorageClient);
    }
}

public sealed class BackOfficeAttachmentDownloadTests : BackOfficeEndpointBaseTest, IClassFixture<BackOfficeAttachmentDownloadFactory>
{
    private const string ContentType = "image/png";
    private static readonly byte[] BlobBytes = "fake-image-bytes"u8.ToArray();
    private readonly IBlobStorageClient _blobStorageClient;

    static BackOfficeAttachmentDownloadTests()
    {
        Environment.SetEnvironmentVariable("BLOB_STORAGE_URL", "https://test.blob.core.windows.net");
    }

    public BackOfficeAttachmentDownloadTests(BackOfficeAttachmentDownloadFactory factory) : base(factory)
    {
        _blobStorageClient = factory.BlobStorageClient;
        _blobStorageClient.ClearReceivedCalls();
        _blobStorageClient.ClearSubstitute();
    }

    [Fact]
    public async Task DownloadAttachment_WhenStaffRequestsTenantAttachment_ShouldStreamFromTenantContainer()
    {
        // Arrange
        const string containerName = "support-tickets";
        const string blobName = "1/HASH123-screenshot.png";
        var (ticketId, messageId) = SeedTicketWithAttachment(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email, containerName, blobName, "screenshot.png");
        _blobStorageClient.DownloadAsync(containerName, blobName, Arg.Any<CancellationToken>())
            .Returns((new MemoryStream(BlobBytes), ContentType));
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/support-tickets/{ticketId}/messages/{messageId}/attachments/HASH123-screenshot.png");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ContentType);
        response.Content.Headers.ContentDisposition!.DispositionType.Should().Be("attachment");
        (await response.Content.ReadAsByteArrayAsync()).Should().Equal(BlobBytes);
    }

    [Fact]
    public async Task DownloadAttachment_WhenStaffRequestsStaffAttachment_ShouldStreamFromStaffContainer()
    {
        // Arrange
        const string containerName = "support-staff";
        const string blobName = "messages/HASH999-staff-doc.pdf";
        var (ticketId, messageId) = SeedTicketWithAttachment(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email, containerName, blobName, "staff-doc.pdf");
        _blobStorageClient.DownloadAsync(containerName, blobName, Arg.Any<CancellationToken>())
            .Returns((new MemoryStream(BlobBytes), "application/pdf"));
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/support-tickets/{ticketId}/messages/{messageId}/attachments/HASH999-staff-doc.pdf");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        await _blobStorageClient.Received(1).DownloadAsync(containerName, blobName, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DownloadAttachment_WhenAnonymous_ShouldReturnUnauthorized()
    {
        // Arrange
        var (ticketId, messageId) = SeedTicketWithAttachment(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email, "support-tickets", "1/HASH123-screenshot.png", "screenshot.png");
        using var client = CreateBackOfficeClient();

        // Act
        var response = await client.GetAsync($"/api/back-office/support-tickets/{ticketId}/messages/{messageId}/attachments/HASH123-screenshot.png");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DownloadAttachment_WhenServedOnUserFacingHost_ShouldNotMatch()
    {
        // Arrange
        var (ticketId, messageId) = SeedTicketWithAttachment(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email, "support-tickets", "1/HASH123-screenshot.png", "screenshot.png");
        using var client = CreateClientForHost("app.test.localhost");

        // Act
        var response = await client.GetAsync($"/api/back-office/support-tickets/{ticketId}/messages/{messageId}/attachments/HASH123-screenshot.png");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DownloadAttachment_WhenFileNameDoesNotMatchAnyAttachment_ShouldReturnNotFound()
    {
        // Arrange
        var (ticketId, messageId) = SeedTicketWithAttachment(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email, "support-tickets", "1/HASH123-screenshot.png", "screenshot.png");
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/support-tickets/{ticketId}/messages/{messageId}/attachments/unknown-file.png");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await _blobStorageClient.DidNotReceiveWithAnyArgs().DownloadAsync(null!, null!, CancellationToken.None);
    }

    private (SupportTicketId TicketId, SupportMessageId MessageId) SeedTicketWithAttachment(TenantId tenantId, UserId reporterId, string reporterEmail, string containerName, string blobName, string originalFileName)
    {
        var ticketId = SupportTicketId.NewId();
        var messageId = SupportMessageId.NewId();
        var now = DateTimeOffset.UtcNow;
        var attachment = new SupportMessageAttachment(originalFileName, ContentType, BlobBytes.Length, $"/{containerName}/{blobName}");
        var message = new SupportMessage(messageId, "staff-oid", SupportMessageAuthorKind.Staff, "Support Staff", "Reply with attachment", [attachment], now);
        var messagesJson = JsonSerializer.Serialize(new[] { message });
        Connection.Insert("support_tickets", [
                ("tenant_id", tenantId.Value),
                ("id", ticketId.ToString()),
                ("created_at", now.AddMinutes(-10)),
                ("modified_at", null),
                ("reporter_id", reporterId.ToString()),
                ("reporter_role_snapshot", nameof(UserRole.Owner)),
                ("reporter_email_snapshot", reporterEmail),
                ("subject", "BO ticket with attachment"),
                ("category", nameof(SupportTicketCategory.Other)),
                ("status", nameof(SupportTicketStatus.AwaitingAgent)),
                ("assignee", null),
                ("last_activity_at", now),
                ("resolved_at", null),
                ("closed_at", null),
                ("csat", null),
                ("messages", messagesJson),
                ("history_events", "[]")
            ]
        );
        return (ticketId, messageId);
    }
}
