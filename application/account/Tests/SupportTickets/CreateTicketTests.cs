using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Account.Database;
using Account.Features.SupportTickets.Domain;
using FluentAssertions;
using SharedKernel.Tests;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.SupportTickets;

public sealed class CreateTicketTests : EndpointBaseTest<AccountDbContext>, IClassFixture<AccountWebApplicationFactory>
{
    public CreateTicketTests(AccountWebApplicationFactory factory) : base(factory)
    {
        Environment.SetEnvironmentVariable("BLOB_STORAGE_URL", "https://test.blob.core.windows.net");
    }

    [Fact]
    public async Task CreateTicket_WhenValid_ShouldPersistTicketAndCollectEvent()
    {
        // Arrange
        var form = BuildCreateForm("Cannot log in to my account", "I'm getting an error when entering my password.", SupportTicketCategory.Account);

        // Act
        var response = await AuthenticatedMemberHttpClient.PostAsync("/api/account/support-tickets", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var ticketId = await response.Content.ReadFromJsonAsync<SupportTicketId>();
        ticketId.Should().NotBeNull();
        var rowCount = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM support_tickets WHERE id = @id AND reporter_id = @reporterId",
            [new { id = ticketId.ToString(), reporterId = DatabaseSeeder.Tenant1Member.Id.ToString() }]
        );
        rowCount.Should().Be(1);
        TelemetryEventsCollectorSpy.CollectedEvents.Should().Contain(e => e.GetType().Name == "SupportTicketCreated");
    }

    [Fact]
    public async Task CreateTicket_WhenSubjectTooShort_ShouldReturnBadRequest()
    {
        // Arrange
        var form = BuildCreateForm("hi", "Body text that is sufficiently long.", SupportTicketCategory.Other);

        // Act
        var response = await AuthenticatedMemberHttpClient.PostAsync("/api/account/support-tickets", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTicket_WhenAttachmentExceedsMaxSize_ShouldReturnBadRequest()
    {
        // Arrange
        var form = BuildCreateForm("Subject for attachment", "A short body to exercise the upload path.", SupportTicketCategory.Bug);
        var oversized = new ByteArrayContent(new byte[26 * 1024 * 1024]);
        oversized.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(oversized, "files", "big.png");

        // Act
        var response = await AuthenticatedMemberHttpClient.PostAsync("/api/account/support-tickets", form);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, "Attachment 'big.png' exceeds the 25 MB maximum size.");
    }

    [Fact]
    public async Task CreateTicket_WhenDisallowedFileType_ShouldReturnBadRequest()
    {
        // Arrange
        var form = BuildCreateForm("Subject for attachment", "A short body to exercise the upload path.", SupportTicketCategory.Bug);
        var disallowed = new ByteArrayContent("malware"u8.ToArray());
        disallowed.Headers.ContentType = new MediaTypeHeaderValue("application/x-msdownload");
        form.Add(disallowed, "files", "evil.exe");

        // Act
        var response = await AuthenticatedMemberHttpClient.PostAsync("/api/account/support-tickets", form);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, "Attachment 'evil.exe' has a disallowed file type.");
    }

    [Fact]
    public async Task CreateTicket_WhenTooManyAttachments_ShouldReturnBadRequest()
    {
        // Arrange
        var form = BuildCreateForm("Subject for attachment", "A short body to exercise the upload path.", SupportTicketCategory.Bug);
        for (var i = 0; i < 6; i++)
        {
            var content = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47]);
            content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            form.Add(content, "files", $"file{i}.png");
        }

        // Act
        var response = await AuthenticatedMemberHttpClient.PostAsync("/api/account/support-tickets", form);

        // Assert
        await response.ShouldHaveErrorStatusCode(HttpStatusCode.BadRequest, "Up to 5 attachments are allowed per message.");
    }

    [Fact]
    public async Task CreateTicket_WhenAnonymous_ShouldReturnUnauthorized()
    {
        // Arrange
        var form = BuildCreateForm("Subject for anonymous test", "Hello world body for anonymous test.", SupportTicketCategory.Other);

        // Act
        var response = await AnonymousHttpClient.PostAsync("/api/account/support-tickets", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static MultipartFormDataContent BuildCreateForm(string subject, string body, SupportTicketCategory category)
    {
        return new MultipartFormDataContent
        {
            { new StringContent(subject), "subject" },
            { new StringContent(body), "body" },
            { new StringContent(category.ToString()), "category" }
        };
    }
}
