using System.Net;
using System.Net.Http.Json;
using Account.Features.SupportTickets.BackOffice.Commands;
using Account.Features.SupportTickets.BackOffice.Queries;
using Account.Features.SupportTickets.Domain;
using Account.Features.Users.Domain;
using Account.Tests.BackOffice;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using SharedKernel.Authentication.MockEasyAuth;
using SharedKernel.Domain;
using SharedKernel.Emails;
using SharedKernel.Integrations.Email;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.SupportTickets.BackOffice;

// A dedicated factory that swaps the per-request transient IEmailClient for a single shared
// substitute so tests can verify Send(...) call counts. The default BackOfficeWebApplicationFactory
// builds a new substitute on every resolution, which makes verification impossible.
public sealed class SupportTicketBackOfficeWebApplicationFactory : BackOfficeWebApplicationFactory
{
    public IEmailClient EmailClient { get; } = Substitute.For<IEmailClient>();

    protected override void ConfigureAdditionalTestServices(IServiceCollection services)
    {
        services.RemoveAll(typeof(IEmailClient));
        services.AddSingleton(EmailClient);

        // The email TSX templates are compiled to dist/ as part of the email build, which doesn't run
        // before the test host starts in CI. Substitute the renderer so handlers don't hit disk.
        services.RemoveAll(typeof(IEmailRenderer));
        var renderer = Substitute.For<IEmailRenderer>();
        renderer.RenderEmail(Arg.Any<EmailTemplateBase>()).Returns(new EmailRenderResult("Subject", "<html />", "Plain"));
        services.AddSingleton(renderer);
    }
}

public sealed class BackOfficeSupportTicketTests(SupportTicketBackOfficeWebApplicationFactory factory)
    : BackOfficeEndpointBaseTest(factory), IClassFixture<SupportTicketBackOfficeWebApplicationFactory>
{
    static BackOfficeSupportTicketTests()
    {
        Environment.SetEnvironmentVariable("BLOB_STORAGE_URL", "https://test.blob.core.windows.net");
    }

    [Fact]
    public async Task GetAllTickets_WhenCalled_ShouldReturnTicketsAcrossTenants()
    {
        // Arrange
        var ticketId = SeedTicket(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email, SupportTicketStatus.New);
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/support-tickets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<AllTicketsResponse>();
        payload.Should().NotBeNull();
        payload.Tickets.Should().Contain(t => t.Id.Value == ticketId.Value);
        payload.Counts.New.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task GetAllTickets_WhenAnonymous_ShouldReturnUnauthorized()
    {
        // Arrange
        using var client = CreateBackOfficeClient();

        // Act
        var response = await client.GetAsync("/api/back-office/support-tickets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // The back-office route group uses BackOfficeIdentityDefaults.PolicyName, which admits any
    // authenticated back-office identity (no group claim required). Per BackOfficeIdentityDefaults
    // the only differentiated outcome between "tenant user" and "back-office staff" is whether the
    // Easy Auth headers are present at all. Anonymous requests get 401, so the PRD's required
    // "non-back-office identity should be rejected" test reduces to verifying every mutation
    // returns 401 on the back-office host without principal headers.
    [Fact]
    public async Task ReplyToTicketAsStaff_WhenAnonymous_ShouldReturnUnauthorized()
    {
        // Arrange
        var ticketId = SeedTicket(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email, SupportTicketStatus.AwaitingAgent);
        using var client = CreateBackOfficeClient();
        var form = new MultipartFormDataContent { { new StringContent("body"), "body" }, { new StringContent("false"), "markAsResolved" } };

        // Act
        var response = await client.PostAsync($"/api/back-office/support-tickets/{ticketId}/reply", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostInternalNote_WhenAnonymous_ShouldReturnUnauthorized()
    {
        // Arrange
        var ticketId = SeedTicket(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email, SupportTicketStatus.AwaitingAgent);
        using var client = CreateBackOfficeClient();
        var form = new MultipartFormDataContent { { new StringContent("internal"), "body" } };

        // Act
        var response = await client.PostAsync($"/api/back-office/support-tickets/{ticketId}/internal-note", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AssignTicket_WhenAnonymous_ShouldReturnUnauthorized()
    {
        // Arrange
        var ticketId = SeedTicket(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email, SupportTicketStatus.New);
        using var client = CreateBackOfficeClient();
        var command = new AssignTicketCommand { AssigneeObjectId = "anyone", AssigneeDisplayName = "Anyone" };

        // Act
        var response = await client.PutAsJsonAsync($"/api/back-office/support-tickets/{ticketId}/assignee", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MarkResolvedByStaff_WhenAnonymous_ShouldReturnUnauthorized()
    {
        // Arrange
        var ticketId = SeedTicket(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email, SupportTicketStatus.AwaitingAgent);
        using var client = CreateBackOfficeClient();

        // Act
        var response = await client.PostAsync($"/api/back-office/support-tickets/{ticketId}/mark-resolved", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetTicketDetailForStaff_WhenAnonymous_ShouldReturnUnauthorized()
    {
        // Arrange
        var ticketId = SeedTicket(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email, SupportTicketStatus.New);
        using var client = CreateBackOfficeClient();

        // Act
        var response = await client.GetAsync($"/api/back-office/support-tickets/{ticketId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReplyToTicketAsStaff_WhenPosted_ShouldTransitionToAwaitingUserAndEnqueueExactlyOneEmail()
    {
        // Arrange
        var ticketId = SeedTicket(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email, SupportTicketStatus.AwaitingAgent);
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);
        var form = new MultipartFormDataContent
        {
            { new StringContent("Hi, we are looking into this now."), "body" },
            { new StringContent("false"), "markAsResolved" }
        };
        factory.EmailClient.ClearReceivedCalls();
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await client.PostAsync($"/api/back-office/support-tickets/{ticketId}/reply", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = Connection.ExecuteScalar<string>("SELECT status FROM support_tickets WHERE id = @id", [new { id = ticketId.ToString() }]);
        status.Should().Be(nameof(SupportTicketStatus.AwaitingUser));
        await factory.EmailClient.Received(1).SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
        TelemetryEventsCollectorSpy.CollectedEvents.Should().Contain(e => e.GetType().Name == "SupportTicketReplyPosted");
    }

    [Fact]
    public async Task ReplyToTicketAsStaff_WhenTicketIsResolvedWithinReopenWindow_ShouldReopenWithReopenedEventAndSendOneEmail()
    {
        // Arrange. Staff reply on a Resolved ticket within the 7-day window must reopen the ticket
        // (emitting the Reopened history event so the customer's chat thread shows the reopen) and
        // emit the SupportTicketReopened telemetry event before the regular reply transition.
        var ticketId = SeedTicket(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email, SupportTicketStatus.Resolved, resolvedAt: DateTimeOffset.UtcNow.AddDays(-2));
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);
        var form = new MultipartFormDataContent
        {
            { new StringContent("Reopening to investigate further."), "body" },
            { new StringContent("false"), "markAsResolved" }
        };
        factory.EmailClient.ClearReceivedCalls();
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await client.PostAsync($"/api/back-office/support-tickets/{ticketId}/reply", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = Connection.ExecuteScalar<string>("SELECT status FROM support_tickets WHERE id = @id", [new { id = ticketId.ToString() }]);
        status.Should().Be(nameof(SupportTicketStatus.AwaitingUser));
        var resolvedAt = Connection.ExecuteScalar<DateTimeOffset?>("SELECT resolved_at FROM support_tickets WHERE id = @id", [new { id = ticketId.ToString() }]);
        resolvedAt.Should().BeNull();
        await factory.EmailClient.Received(1).SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
        TelemetryEventsCollectorSpy.CollectedEvents.Should().Contain(e => e.GetType().Name == "SupportTicketReopened");
        var historyJson = Connection.ExecuteScalar<string>("SELECT history_events FROM support_tickets WHERE id = @id", [new { id = ticketId.ToString() }]);
        historyJson.Should().Contain("Reopened");
    }

    [Fact]
    public async Task ReplyToTicketAsStaff_WhenTicketIsResolvedPastReopenWindow_ShouldReturnBadRequestAndNotSendEmail()
    {
        // Arrange. Resolved 8 days ago is past the 7-day reopen window; the staff reply must be
        // rejected rather than silently reopening the ticket and emailing the customer.
        var ticketId = SeedTicket(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email, SupportTicketStatus.Resolved, resolvedAt: DateTimeOffset.UtcNow.AddDays(-8));
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);
        var form = new MultipartFormDataContent
        {
            { new StringContent("Trying to reply past the reopen window."), "body" },
            { new StringContent("false"), "markAsResolved" }
        };
        factory.EmailClient.ClearReceivedCalls();

        // Act
        var response = await client.PostAsync($"/api/back-office/support-tickets/{ticketId}/reply", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var status = Connection.ExecuteScalar<string>("SELECT status FROM support_tickets WHERE id = @id", [new { id = ticketId.ToString() }]);
        status.Should().Be(nameof(SupportTicketStatus.Resolved));
        await factory.EmailClient.DidNotReceive().SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PostInternalNote_WhenPosted_ShouldNotChangeStatusAndNotEnqueueEmail()
    {
        // Arrange
        var ticketId = SeedTicket(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email, SupportTicketStatus.AwaitingAgent);
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);
        var form = new MultipartFormDataContent
        {
            { new StringContent("Looking into the upstream provider. Do not share."), "body" }
        };
        factory.EmailClient.ClearReceivedCalls();

        // Act
        var response = await client.PostAsync($"/api/back-office/support-tickets/{ticketId}/internal-note", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = Connection.ExecuteScalar<string>("SELECT status FROM support_tickets WHERE id = @id", [new { id = ticketId.ToString() }]);
        status.Should().Be(nameof(SupportTicketStatus.AwaitingAgent));
        await factory.EmailClient.DidNotReceiveWithAnyArgs().SendAsync(null!, CancellationToken.None);
    }

    [Fact]
    public async Task AssignTicket_WhenStaffAssignsToSelf_ShouldPersistAssignee()
    {
        // Arrange
        var ticketId = SeedTicket(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email, SupportTicketStatus.New);
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);
        var command = new AssignTicketCommand { AssigneeObjectId = identity.ObjectId, AssigneeDisplayName = identity.Name };

        // Act
        var response = await client.PutAsJsonAsync($"/api/back-office/support-tickets/{ticketId}/assignee", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var assigneeJson = Connection.ExecuteScalar<string>("SELECT assignee FROM support_tickets WHERE id = @id", [new { id = ticketId.ToString() }]);
        assigneeJson.Should().Contain(identity.ObjectId);
    }

    [Fact]
    public async Task GetAllTickets_WhenFilteredByReporterId_ShouldReturnOnlyThatReportersTickets()
    {
        // Arrange
        var ownerTicketId = SeedTicket(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email, SupportTicketStatus.AwaitingAgent);
        var memberTicketId = SeedTicket(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Member.Id, DatabaseSeeder.Tenant1Member.Email, SupportTicketStatus.AwaitingAgent);
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/support-tickets?ReporterId={DatabaseSeeder.Tenant1Owner.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<AllTicketsResponse>();
        payload.Should().NotBeNull();
        payload.Tickets.Should().Contain(t => t.Id.Value == ownerTicketId.Value);
        payload.Tickets.Should().NotContain(t => t.Id.Value == memberTicketId.Value);
    }

    [Fact]
    public async Task GetAllTickets_WhenFilteredByTenantId_ShouldReturnOnlyThatTenantsTickets()
    {
        // Arrange
        var tenant1TicketId = SeedTicket(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email, SupportTicketStatus.AwaitingAgent);
        var otherTenantId = SeedOtherTenant();
        var otherTenantTicketId = SeedTicket(otherTenantId, UserId.NewId(), "other@tenant.example", SupportTicketStatus.AwaitingAgent);
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/support-tickets?TenantId={DatabaseSeeder.Tenant1.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<AllTicketsResponse>();
        payload.Should().NotBeNull();
        payload.Tickets.Should().Contain(t => t.Id.Value == tenant1TicketId.Value);
        payload.Tickets.Should().NotContain(t => t.Id.Value == otherTenantTicketId.Value);
    }

    [Fact]
    public async Task GetAllTickets_WhenSearchMatchesMessageBody_ShouldReturnTicket()
    {
        // Arrange
        var ticketId = SeedTicket(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email, SupportTicketStatus.AwaitingAgent);
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);
        const string uniqueMarker = "needle-in-haystack-marker";
        var form = new MultipartFormDataContent
        {
            { new StringContent($"Hi, here is the {uniqueMarker} for you."), "body" },
            { new StringContent("false"), "markAsResolved" }
        };
        var replyResponse = await client.PostAsync($"/api/back-office/support-tickets/{ticketId}/reply", form);
        replyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        var response = await client.GetAsync($"/api/back-office/support-tickets?Search={uniqueMarker}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<AllTicketsResponse>();
        payload.Should().NotBeNull();
        payload.Tickets.Should().Contain(t => t.Id.Value == ticketId.Value);
    }

    [Fact]
    public async Task GetAllTickets_WhenSearchMatchesTenantName_ShouldReturnTicket()
    {
        // Arrange
        var ticketId = SeedTicket(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email, SupportTicketStatus.New);
        const string uniqueTenantName = "Acme-Search-Tenant";
        Connection.Update("tenants", "id", DatabaseSeeder.Tenant1.Id.ToString(), [("name", uniqueTenantName)]);
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/support-tickets?Search={Uri.EscapeDataString("Acme-Search")}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<AllTicketsResponse>();
        payload.Should().NotBeNull();
        payload.Tickets.Should().Contain(t => t.Id.Value == ticketId.Value);
    }

    [Fact]
    public async Task GetAllTickets_WhenSortedBySubjectAscending_ShouldReturnAlphabeticalOrder()
    {
        // Arrange
        var zebraId = SeedTicket(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email, SupportTicketStatus.New, "Zebra crossing");
        var alphaId = SeedTicket(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email, SupportTicketStatus.New, "Alpha bug");
        var middleId = SeedTicket(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email, SupportTicketStatus.New, "Middle issue");
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/support-tickets?OrderBy=Subject&SortOrder=Ascending");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<AllTicketsResponse>();
        payload.Should().NotBeNull();
        var orderedSubjects = payload.Tickets
            .Where(t => t.Id.Value == alphaId.Value || t.Id.Value == middleId.Value || t.Id.Value == zebraId.Value)
            .Select(t => t.Subject)
            .ToArray();
        orderedSubjects.Should().Equal("Alpha bug", "Middle issue", "Zebra crossing");
    }

    [Theory]
    [InlineData("Ascending")]
    [InlineData("Descending")]
    public async Task GetAllTickets_WhenSortedByCsat_ShouldKeepUnratedTicketsAtEnd(string sortOrder)
    {
        // Arrange
        var ratedId = SeedTicket(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email, SupportTicketStatus.Resolved, "Rated ticket");
        var unratedId = SeedTicket(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email, SupportTicketStatus.Resolved, "Unrated ticket");
        var csatJson = $"{{\"Score\":\"Helpful\",\"Comment\":null,\"SubmittedAt\":\"{DateTimeOffset.UtcNow:O}\"}}";
        Connection.Update("support_tickets", "id", ratedId.ToString(), [("csat", csatJson)]);
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/support-tickets?OrderBy=Csat&SortOrder={sortOrder}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<AllTicketsResponse>();
        payload.Should().NotBeNull();
        var ratedIndex = Array.FindIndex(payload.Tickets, t => t.Id.Value == ratedId.Value);
        var unratedIndex = Array.FindIndex(payload.Tickets, t => t.Id.Value == unratedId.Value);
        ratedIndex.Should().BeLessThan(unratedIndex);
    }

    [Theory]
    [InlineData("Ascending")]
    [InlineData("Descending")]
    public async Task GetAllTickets_WhenSortedByAssignee_ShouldKeepUnassignedTicketsAtEnd(string sortOrder)
    {
        // Arrange
        var assignedId = SeedTicket(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email, SupportTicketStatus.AwaitingAgent, "Assigned ticket");
        var unassignedId = SeedTicket(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email, SupportTicketStatus.AwaitingAgent, "Unassigned ticket");
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);
        var assignCommand = new AssignTicketCommand { AssigneeObjectId = identity.ObjectId, AssigneeDisplayName = identity.Name };
        var assignResponse = await client.PutAsJsonAsync($"/api/back-office/support-tickets/{assignedId}/assignee", assignCommand);
        assignResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        var response = await client.GetAsync($"/api/back-office/support-tickets?OrderBy=Assignee&SortOrder={sortOrder}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<AllTicketsResponse>();
        payload.Should().NotBeNull();
        var assignedIndex = Array.FindIndex(payload.Tickets, t => t.Id.Value == assignedId.Value);
        var unassignedIndex = Array.FindIndex(payload.Tickets, t => t.Id.Value == unassignedId.Value);
        assignedIndex.Should().BeLessThan(unassignedIndex);
    }

    [Fact]
    public async Task GetAllTickets_WhenResolvedWithFreshCsat_ShouldReportComputedStatusAsClosed()
    {
        // Arrange. A rated Resolved ticket is computed-Closed because the reporter has signed off,
        // so the inbox summary should surface Closed in the Status column.
        var csatJson = $"{{\"Score\":\"Helpful\",\"Comment\":null,\"SubmittedAt\":\"{DateTimeOffset.UtcNow:O}\"}}";
        var ticketId = SeedTicket(
            DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email, SupportTicketStatus.Resolved,
            "Rated resolved ticket", DateTimeOffset.UtcNow.AddHours(-1), csatJson
        );
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/support-tickets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<AllTicketsResponse>();
        payload.Should().NotBeNull();
        payload.Tickets.Should().Contain(t => t.Id.Value == ticketId.Value && t.Status == SupportTicketStatus.Closed);
    }

    [Fact]
    public async Task GetAllTickets_WhenResolvedPastReopenWindow_ShouldReportComputedStatusAsClosed()
    {
        // Arrange. A Resolved ticket past the 7-day reopen window without a rating is also
        // computed-Closed because the reopen affordance is gone.
        var ticketId = SeedTicket(
            DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email, SupportTicketStatus.Resolved,
            "Aged resolved ticket", DateTimeOffset.UtcNow.AddDays(-8)
        );
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/support-tickets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<AllTicketsResponse>();
        payload.Should().NotBeNull();
        payload.Tickets.Should().Contain(t => t.Id.Value == ticketId.Value && t.Status == SupportTicketStatus.Closed);
    }

    [Fact]
    public async Task GetAllTickets_WhenFilteringByClosed_ShouldIncludeComputedClosedTickets()
    {
        // Arrange. Filtering Status=Closed must match both legacy Closed rows AND rated/aged
        // Resolved rows that compute to Closed.
        var csatJson = $"{{\"Score\":\"Helpful\",\"Comment\":null,\"SubmittedAt\":\"{DateTimeOffset.UtcNow:O}\"}}";
        var ratedId = SeedTicket(
            DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email, SupportTicketStatus.Resolved,
            "Rated resolved ticket", DateTimeOffset.UtcNow.AddHours(-1), csatJson
        );
        var rawResolvedId = SeedTicket(
            DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email, SupportTicketStatus.Resolved,
            "Unrated recent resolved ticket", DateTimeOffset.UtcNow.AddHours(-1)
        );
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/support-tickets?Status=Closed");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<AllTicketsResponse>();
        payload.Should().NotBeNull();
        payload.Tickets.Should().Contain(t => t.Id.Value == ratedId.Value);
        payload.Tickets.Should().NotContain(t => t.Id.Value == rawResolvedId.Value);
    }

    [Fact]
    public async Task GetAllTickets_WhenResolvedRecently_ShouldCountInResolvedLast24HoursRegardlessOfRating()
    {
        // Arrange. The Resolved tile counts resolution events in the last 24 hours regardless of
        // whether the reporter has since tipped the row into computed Closed via a CSAT rating.
        var csatJson = $"{{\"Score\":\"Helpful\",\"Comment\":null,\"SubmittedAt\":\"{DateTimeOffset.UtcNow:O}\"}}";
        SeedTicket(
            DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email, SupportTicketStatus.Resolved,
            "Rated resolved ticket within window", DateTimeOffset.UtcNow.AddHours(-2), csatJson
        );
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/support-tickets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<AllTicketsResponse>();
        payload.Should().NotBeNull();
        payload.Counts.ResolvedLast24Hours.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task ReplyToTicketAsStaff_WhenTicketIsLegacyClosed_ShouldReopenAndAppend()
    {
        // Arrange. A legacy Closed row (the only path that still writes Status=Closed is historical
        // data) is always reopenable. A staff reply must reopen it rather than be rejected.
        var ticketId = SeedTicket(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email, SupportTicketStatus.Closed);
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);
        var form = new MultipartFormDataContent
        {
            { new StringContent("Following up on this old thread."), "body" },
            { new StringContent("false"), "markAsResolved" }
        };
        factory.EmailClient.ClearReceivedCalls();

        // Act
        var response = await client.PostAsync($"/api/back-office/support-tickets/{ticketId}/reply", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = Connection.ExecuteScalar<string>("SELECT status FROM support_tickets WHERE id = @id", [new { id = ticketId.ToString() }]);
        status.Should().Be(nameof(SupportTicketStatus.AwaitingUser));
        var historyJson = Connection.ExecuteScalar<string>("SELECT history_events FROM support_tickets WHERE id = @id", [new { id = ticketId.ToString() }]);
        historyJson.Should().Contain("Reopened");
    }

    [Fact]
    public async Task MarkResolvedByStaff_WhenAwaitingAgent_ShouldTransitionToResolved()
    {
        // Arrange
        var ticketId = SeedTicket(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email, SupportTicketStatus.AwaitingAgent);
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.PostAsync($"/api/back-office/support-tickets/{ticketId}/mark-resolved", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = Connection.ExecuteScalar<string>("SELECT status FROM support_tickets WHERE id = @id", [new { id = ticketId.ToString() }]);
        status.Should().Be(nameof(SupportTicketStatus.Resolved));
        var resolvedAt = Connection.ExecuteScalar<string>("SELECT resolved_at FROM support_tickets WHERE id = @id", [new { id = ticketId.ToString() }]);
        resolvedAt.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task AssignTicket_WhenClearedWithNull_ShouldRemoveAssignee()
    {
        // Arrange. Unassigning sends a null AssigneeObjectId; the cleared assignee must persist as a
        // SQL NULL rather than silently retaining the previous staff member.
        var ticketId = SeedTicket(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email, SupportTicketStatus.AwaitingAgent);
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);
        await client.PutAsJsonAsync($"/api/back-office/support-tickets/{ticketId}/assignee", new AssignTicketCommand { AssigneeObjectId = identity.ObjectId, AssigneeDisplayName = identity.Name });

        // Act
        var response = await client.PutAsJsonAsync($"/api/back-office/support-tickets/{ticketId}/assignee", new AssignTicketCommand { AssigneeObjectId = null, AssigneeDisplayName = null });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var assigneeJson = Connection.ExecuteScalar<string?>("SELECT assignee FROM support_tickets WHERE id = @id", [new { id = ticketId.ToString() }]);
        assigneeJson.Should().BeNull();
    }

    [Fact]
    public async Task AssignTicket_WhenAssignedToSameStaffTwice_ShouldReturnBadRequest()
    {
        // Arrange
        var ticketId = SeedTicket(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email, SupportTicketStatus.AwaitingAgent);
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);
        var command = new AssignTicketCommand { AssigneeObjectId = identity.ObjectId, AssigneeDisplayName = identity.Name };
        await client.PutAsJsonAsync($"/api/back-office/support-tickets/{ticketId}/assignee", command);

        // Act
        var response = await client.PutAsJsonAsync($"/api/back-office/support-tickets/{ticketId}/assignee", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AssignTicket_WhenReassignedToDifferentStaff_ShouldUpdateAssignee()
    {
        // Arrange
        var ticketId = SeedTicket(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email, SupportTicketStatus.AwaitingAgent);
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);
        await client.PutAsJsonAsync($"/api/back-office/support-tickets/{ticketId}/assignee", new AssignTicketCommand { AssigneeObjectId = identity.ObjectId, AssigneeDisplayName = identity.Name });

        // Act
        var response = await client.PutAsJsonAsync($"/api/back-office/support-tickets/{ticketId}/assignee", new AssignTicketCommand { AssigneeObjectId = "other-staff-object-id", AssigneeDisplayName = "Other Staff" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var assigneeJson = Connection.ExecuteScalar<string>("SELECT assignee FROM support_tickets WHERE id = @id", [new { id = ticketId.ToString() }]);
        assigneeJson.Should().Contain("other-staff-object-id");
    }

    [Fact]
    public async Task GetAllTickets_WhenSearchMatchesInternalNoteBody_ShouldReturnTicket()
    {
        // Arrange. Search must match internal-note bodies — they are staff-visible, so the inbox
        // search deliberately indexes them (contrast with the reporter-facing surfaces).
        var ticketId = SeedTicket(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email, SupportTicketStatus.AwaitingAgent);
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);
        const string uniqueMarker = "internal-only-marker-zzz";
        var form = new MultipartFormDataContent { { new StringContent($"Internal: {uniqueMarker}"), "body" } };
        var noteResponse = await client.PostAsync($"/api/back-office/support-tickets/{ticketId}/internal-note", form);
        noteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act
        var response = await client.GetAsync($"/api/back-office/support-tickets?Search={uniqueMarker}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<AllTicketsResponse>();
        payload.Should().NotBeNull();
        payload.Tickets.Should().Contain(t => t.Id.Value == ticketId.Value);
    }

    [Fact]
    public async Task GetAllTickets_WhenFilteredByAssigneeMe_ShouldReturnOnlyOwnAssignedTickets()
    {
        // Arrange. The "my queue" filter resolves "Me" from the staff NameIdentifier claim.
        var mineTicketId = SeedTicket(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email, SupportTicketStatus.AwaitingAgent);
        var unassignedTicketId = SeedTicket(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Member.Id, DatabaseSeeder.Tenant1Member.Email, SupportTicketStatus.AwaitingAgent);
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);
        await client.PutAsJsonAsync($"/api/back-office/support-tickets/{mineTicketId}/assignee", new AssignTicketCommand { AssigneeObjectId = identity.ObjectId, AssigneeDisplayName = identity.Name });

        // Act
        var response = await client.GetAsync("/api/back-office/support-tickets?Assignee=Me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<AllTicketsResponse>();
        payload.Should().NotBeNull();
        payload.Tickets.Should().Contain(t => t.Id.Value == mineTicketId.Value);
        payload.Tickets.Should().NotContain(t => t.Id.Value == unassignedTicketId.Value);
    }

    [Fact]
    public async Task GetTicketDetailForStaff_WhenTicketExists_ShouldReturnDetailIncludingInternalNotes()
    {
        // Arrange. Unlike the reporter detail query, the staff detail must surface internal notes.
        var ticketId = SeedTicket(DatabaseSeeder.Tenant1.Id, DatabaseSeeder.Tenant1Owner.Id, DatabaseSeeder.Tenant1Owner.Email, SupportTicketStatus.AwaitingAgent);
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);
        var form = new MultipartFormDataContent { { new StringContent("Staff-only context."), "body" } };
        await client.PostAsync($"/api/back-office/support-tickets/{ticketId}/internal-note", form);

        // Act
        var response = await client.GetAsync($"/api/back-office/support-tickets/{ticketId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<StaffTicketDetailResponse>();
        payload.Should().NotBeNull();
        payload.Id.Value.Should().Be(ticketId.Value);
        payload.Messages.Should().Contain(m => m.AuthorKind == SupportMessageAuthorKind.Internal);
    }

    [Fact]
    public async Task GetTicketDetailForStaff_WhenTicketBelongsToAnotherTenant_ShouldReturnDetail()
    {
        // Arrange. Back-office staff are cross-tenant by design; the staff detail query uses an
        // unfiltered fetch and must return tickets regardless of tenant.
        var otherTenantId = SeedOtherTenant();
        var ticketId = SeedTicket(otherTenantId, UserId.NewId(), "other@tenant.example", SupportTicketStatus.AwaitingAgent);
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/support-tickets/{ticketId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<StaffTicketDetailResponse>();
        payload.Should().NotBeNull();
        payload.Id.Value.Should().Be(ticketId.Value);
    }

    [Fact]
    public async Task GetTicketDetailForStaff_WhenTicketDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync($"/api/back-office/support-tickets/{SupportTicketId.NewId()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAllTickets_WhenPageOffsetExceedsTotalPages_ShouldReturnBadRequest()
    {
        // Arrange. An out-of-range page must be rejected rather than silently returning an empty page.
        var identity = MockEasyAuthIdentities.Default.Single(i => i.Id == "user");
        using var client = CreateBackOfficeClientForIdentity(identity);

        // Act
        var response = await client.GetAsync("/api/back-office/support-tickets?PageOffset=999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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

    private SupportTicketId SeedTicket(
        TenantId tenantId,
        UserId reporterId,
        string reporterEmail,
        SupportTicketStatus status,
        string subject = "Seeded support ticket",
        DateTimeOffset? resolvedAt = null,
        string? csatJson = null
    )
    {
        var id = SupportTicketId.NewId();
        var now = DateTimeOffset.UtcNow;
        Connection.Insert("support_tickets", [
                ("tenant_id", tenantId.Value),
                ("id", id.ToString()),
                ("created_at", now.AddMinutes(-30)),
                ("modified_at", null),
                ("reporter_id", reporterId.ToString()),
                ("reporter_role_snapshot", nameof(UserRole.Owner)),
                ("reporter_email_snapshot", reporterEmail),
                ("subject", subject),
                ("category", nameof(SupportTicketCategory.Account)),
                ("status", status.ToString()),
                ("assignee", null),
                ("last_activity_at", now),
                ("resolved_at", resolvedAt),
                ("closed_at", null),
                ("csat", csatJson),
                ("messages", "[]"),
                ("history_events", "[]")
            ]
        );
        return id;
    }
}
