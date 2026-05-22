using System.Net;
using System.Text.Json;
using Account.Database;
using Account.Features.SupportTickets.Domain;
using Account.Features.Users.Domain;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using NSubstitute.ClearExtensions;
using SharedKernel.Domain;
using SharedKernel.Integrations.BlobStorage;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.SupportTickets;

public sealed class AttachmentDownloadFactory : AccountWebApplicationFactory
{
    public IBlobStorageClient BlobStorageClient { get; } = Substitute.For<IBlobStorageClient>();

    protected override void ConfigureAdditionalTestServices(IServiceCollection services)
    {
        services.RemoveAll(typeof(IBlobStorageClient));
        services.AddKeyedSingleton("account-storage", BlobStorageClient);
    }
}

public sealed class AttachmentDownloadTests : EndpointBaseTest<AccountDbContext>, IClassFixture<AttachmentDownloadFactory>
{
    private const string ContainerName = "support-tickets";
    private const string BlobName = "1/HASH123-screenshot.png";
    private const string ContentType = "image/png";
    private static readonly byte[] BlobBytes = "fake-image-bytes"u8.ToArray();
    private readonly IBlobStorageClient _blobStorageClient;

    public AttachmentDownloadTests(AttachmentDownloadFactory factory) : base(factory)
    {
        Environment.SetEnvironmentVariable("BLOB_STORAGE_URL", "https://test.blob.core.windows.net");
        _blobStorageClient = factory.BlobStorageClient;
        _blobStorageClient.ClearReceivedCalls();
        _blobStorageClient.ClearSubstitute();
    }

    [Fact]
    public async Task DownloadAttachment_WhenReporter_ShouldStreamBlobAndForceAttachmentDisposition()
    {
        // Arrange
        var (ticketId, messageId) = SeedTicketWithAttachment(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email);
        _blobStorageClient.DownloadAsync(ContainerName, BlobName, Arg.Any<CancellationToken>())
            .Returns((new MemoryStream(BlobBytes), ContentType));

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/api/account/support-tickets/{ticketId}/messages/{messageId}/attachments/HASH123-screenshot.png");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be(ContentType);
        response.Content.Headers.ContentDisposition!.DispositionType.Should().Be("attachment");
        response.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");
        (await response.Content.ReadAsByteArrayAsync()).Should().Equal(BlobBytes);
    }

    [Fact]
    public async Task DownloadAttachment_WhenFileNameDoesNotMatchAnyAttachment_ShouldReturnNotFound()
    {
        // Arrange
        var (ticketId, messageId) = SeedTicketWithAttachment(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email);

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/api/account/support-tickets/{ticketId}/messages/{messageId}/attachments/unknown-file.png");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await _blobStorageClient.DidNotReceiveWithAnyArgs().DownloadAsync(null!, null!, CancellationToken.None);
    }

    [Fact]
    public async Task DownloadAttachment_WhenTicketBelongsToAnotherTenant_ShouldReturnNotFound()
    {
        // Arrange
        var otherTenantId = SeedOtherTenant();
        var (ticketId, messageId) = SeedTicketWithAttachment(otherTenantId, UserId.NewId(), "other@tenant.example");

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/api/account/support-tickets/{ticketId}/messages/{messageId}/attachments/HASH123-screenshot.png");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await _blobStorageClient.DidNotReceiveWithAnyArgs().DownloadAsync(null!, null!, CancellationToken.None);
    }

    [Fact]
    public async Task DownloadAttachment_WhenReporterIsAnotherUserInSameTenant_ShouldReturnNotFound()
    {
        // Arrange
        var (ticketId, messageId) = SeedTicketWithAttachment(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email);

        // Act
        var response = await AuthenticatedMemberHttpClient.GetAsync($"/api/account/support-tickets/{ticketId}/messages/{messageId}/attachments/HASH123-screenshot.png");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await _blobStorageClient.DidNotReceiveWithAnyArgs().DownloadAsync(null!, null!, CancellationToken.None);
    }

    [Fact]
    public async Task DownloadAttachment_WhenAnonymous_ShouldReturnUnauthorized()
    {
        // Arrange
        var (ticketId, messageId) = SeedTicketWithAttachment(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email);

        // Act
        var response = await AnonymousHttpClient.GetAsync($"/api/account/support-tickets/{ticketId}/messages/{messageId}/attachments/HASH123-screenshot.png");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DownloadAttachment_WhenReporterAndMessageIsInternalNote_ShouldReturnNotFound()
    {
        // Arrange. The reporter owns the ticket, but the attachment is on a staff-only internal note
        // stored in the support-staff container. The reporter download endpoint must never serve it,
        // even given a valid internal SupportMessageId.
        var (ticketId, internalMessageId) = SeedTicketWithInternalNoteAttachment(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email);

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/api/account/support-tickets/{ticketId}/messages/{internalMessageId}/attachments/HASH999-private.png");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await _blobStorageClient.DidNotReceiveWithAnyArgs().DownloadAsync(null!, null!, CancellationToken.None);
    }

    private (SupportTicketId TicketId, SupportMessageId MessageId) SeedTicketWithAttachment(TenantId tenantId, UserId reporterId, string reporterEmail)
    {
        var ticketId = SupportTicketId.NewId();
        var messageId = SupportMessageId.NewId();
        var now = DateTimeOffset.UtcNow;
        var attachment = new SupportMessageAttachment("screenshot.png", ContentType, BlobBytes.Length, $"/{ContainerName}/{BlobName}");
        var message = new SupportMessage(messageId, reporterId.Value, SupportMessageAuthorKind.User, reporterEmail, "Initial message", [attachment], now);
        var messagesJson = JsonSerializer.Serialize(new[] { message });
        Connection.Insert("support_tickets", [
                ("tenant_id", tenantId.Value),
                ("id", ticketId.ToString()),
                ("created_at", now.AddMinutes(-10)),
                ("modified_at", null),
                ("reporter_id", reporterId.ToString()),
                ("reporter_role_snapshot", nameof(UserRole.Owner)),
                ("reporter_email_snapshot", reporterEmail),
                ("subject", "Ticket with attachment"),
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

    private (SupportTicketId TicketId, SupportMessageId InternalMessageId) SeedTicketWithInternalNoteAttachment(TenantId tenantId, UserId reporterId, string reporterEmail)
    {
        var ticketId = SupportTicketId.NewId();
        var internalMessageId = SupportMessageId.NewId();
        var now = DateTimeOffset.UtcNow;
        var internalAttachment = new SupportMessageAttachment("private.png", ContentType, BlobBytes.Length, "/support-staff/messages/HASH999-private.png");
        var internalNote = new SupportMessage(internalMessageId, "staff-oid", SupportMessageAuthorKind.Internal, "Support Staff", "Internal triage note", [internalAttachment], now);
        var messagesJson = JsonSerializer.Serialize(new[] { internalNote });
        Connection.Insert("support_tickets", [
                ("tenant_id", tenantId.Value),
                ("id", ticketId.ToString()),
                ("created_at", now.AddMinutes(-10)),
                ("modified_at", null),
                ("reporter_id", reporterId.ToString()),
                ("reporter_role_snapshot", nameof(UserRole.Owner)),
                ("reporter_email_snapshot", reporterEmail),
                ("subject", "Ticket with internal-note attachment"),
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
        return (ticketId, internalMessageId);
    }

    private TenantId SeedOtherTenant()
    {
        var otherTenantId = DatabaseSeeder.Tenant1.Id.Value + 9999;
        var now = DateTimeOffset.UtcNow;
        Connection.Insert("tenants", [
                ("id", otherTenantId),
                ("created_at", now),
                ("modified_at", null),
                ("deleted_at", null),
                ("name", "Other Tenant"),
                ("state", "Active"),
                ("plan", "Basis"),
                ("suspension_reason", null),
                ("suspended_at", null),
                ("logo", "{}"),
                ("rollout_bucket", 0),
                ("ab_inclusion_pin", null)
            ]
        );
        return new TenantId(otherTenantId);
    }
}
