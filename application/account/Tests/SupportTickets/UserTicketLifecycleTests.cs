using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Account.Database;
using Account.Features.SupportTickets.Commands;
using Account.Features.SupportTickets.Domain;
using Account.Features.SupportTickets.Queries;
using FluentAssertions;
using SharedKernel.Domain;
using SharedKernel.Tests.Persistence;
using Xunit;

namespace Account.Tests.SupportTickets;

public sealed class UserTicketLifecycleTests : EndpointBaseTest<AccountDbContext>, IClassFixture<AccountWebApplicationFactory>
{
    public UserTicketLifecycleTests(AccountWebApplicationFactory factory) : base(factory)
    {
        Environment.SetEnvironmentVariable("BLOB_STORAGE_URL", "https://test.blob.core.windows.net");
    }

    [Fact]
    public async Task ReplyToTicketAsUser_WhenPostedToOwnTicket_ShouldAppendMessageAndTransitionToAwaitingAgent()
    {
        // Arrange
        var ticketId = await CreateTicketViaApi();
        SetTicketStatus(ticketId, SupportTicketStatus.AwaitingUser);
        TelemetryEventsCollectorSpy.Reset();
        var form = new MultipartFormDataContent
        {
            { new StringContent("Thanks for the update. Here is additional info."), "body" },
            { new StringContent("false"), "markAsResolved" }
        };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsync($"/api/account/support-tickets/{ticketId}/reply", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = Connection.ExecuteScalar<string>("SELECT status FROM support_tickets WHERE id = @id", [new { id = ticketId.ToString() }]);
        status.Should().Be(nameof(SupportTicketStatus.AwaitingAgent));
        TelemetryEventsCollectorSpy.CollectedEvents.Should().Contain(e => e.GetType().Name == "SupportTicketReplyPosted");
    }

    [Fact]
    public async Task ReplyToTicketAsUser_WhenReporterIsAnotherUserInSameTenant_ShouldReturnNotFound()
    {
        // Arrange
        var ticketId = await CreateTicketViaApi(); // Created by Tenant1Owner
        var form = new MultipartFormDataContent
        {
            { new StringContent("I should not be able to reply to someone else's ticket."), "body" },
            { new StringContent("false"), "markAsResolved" }
        };

        // Act
        var response = await AuthenticatedMemberHttpClient.PostAsync($"/api/account/support-tickets/{ticketId}/reply", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SubmitCsat_WhenReporterIsAnotherUserInSameTenant_ShouldReturnNotFound()
    {
        // Arrange. CSAT submission is gated by the same reporter-ownership check as replies, so a
        // different user in the same tenant must not be able to rate someone else's ticket.
        var ticketId = await CreateTicketViaApi(); // Created by Tenant1Owner
        var rateCommand = new SubmitCsatCommand(SupportTicketCsatScore.Helpful, null);

        // Act
        var response = await AuthenticatedMemberHttpClient.PostAsJsonAsync($"/api/account/support-tickets/{ticketId}/csat", rateCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReplyToTicketAsUser_WhenStatusIsResolvedWithinReopenWindow_ShouldReopenWithReopenedEventAndAppend()
    {
        // Arrange. The ticket is Resolved inside the 7-day reopen window. A user reply must reopen
        // the ticket, emit the Reopened history event (so CSAT staleness derives correctly), and
        // emit the SupportTicketReopened telemetry event.
        var ticketId = await CreateTicketViaApi();
        SetTicketStatus(ticketId, SupportTicketStatus.Resolved);
        SetResolvedAt(ticketId, DateTimeOffset.UtcNow.AddDays(-2));
        TelemetryEventsCollectorSpy.Reset();
        var form = new MultipartFormDataContent
        {
            { new StringContent("Actually still broken; reopening with more detail."), "body" },
            { new StringContent("false"), "markAsResolved" }
        };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsync($"/api/account/support-tickets/{ticketId}/reply", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = Connection.ExecuteScalar<string>("SELECT status FROM support_tickets WHERE id = @id", [new { id = ticketId.ToString() }]);
        status.Should().Be(nameof(SupportTicketStatus.AwaitingAgent));
        var resolvedAt = Connection.ExecuteScalar<DateTimeOffset?>("SELECT resolved_at FROM support_tickets WHERE id = @id", [new { id = ticketId.ToString() }]);
        resolvedAt.Should().BeNull();
        TelemetryEventsCollectorSpy.CollectedEvents.Should().Contain(e => e.GetType().Name == "SupportTicketReopened");
        var historyJson = Connection.ExecuteScalar<string>("SELECT history_events FROM support_tickets WHERE id = @id", [new { id = ticketId.ToString() }]);
        historyJson.Should().Contain("Reopened");
    }

    [Fact]
    public async Task ReplyToTicketAsUser_WhenStatusIsLegacyClosed_ShouldReopenWithReopenedEventAndAppend()
    {
        // Arrange. A legacy Closed row is always reopenable (no 7-day window applies to Closed). A
        // user reply must reopen it and emit the Reopened history event.
        var ticketId = await CreateTicketViaApi();
        SetTicketStatus(ticketId, SupportTicketStatus.Closed);
        TelemetryEventsCollectorSpy.Reset();
        var form = new MultipartFormDataContent
        {
            { new StringContent("Reopening this old closed ticket."), "body" },
            { new StringContent("false"), "markAsResolved" }
        };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsync($"/api/account/support-tickets/{ticketId}/reply", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = Connection.ExecuteScalar<string>("SELECT status FROM support_tickets WHERE id = @id", [new { id = ticketId.ToString() }]);
        status.Should().Be(nameof(SupportTicketStatus.AwaitingAgent));
        TelemetryEventsCollectorSpy.CollectedEvents.Should().Contain(e => e.GetType().Name == "SupportTicketReopened");
        var historyJson = Connection.ExecuteScalar<string>("SELECT history_events FROM support_tickets WHERE id = @id", [new { id = ticketId.ToString() }]);
        historyJson.Should().Contain("Reopened");
    }

    [Fact]
    public async Task ReplyToTicketAsUser_WhenStatusIsResolvedPastReopenWindow_ShouldReturnBadRequestAndKeepResolved()
    {
        // Arrange. The ticket is Resolved 8 days ago, past the 7-day reopen window. The handler must
        // reject the reply rather than silently reopen the ticket.
        var ticketId = await CreateTicketViaApi();
        SetTicketStatus(ticketId, SupportTicketStatus.Resolved);
        SetResolvedAt(ticketId, DateTimeOffset.UtcNow.AddDays(-8));
        var form = new MultipartFormDataContent
        {
            { new StringContent("Trying to reply past the reopen window."), "body" },
            { new StringContent("false"), "markAsResolved" }
        };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsync($"/api/account/support-tickets/{ticketId}/reply", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var status = Connection.ExecuteScalar<string>("SELECT status FROM support_tickets WHERE id = @id", [new { id = ticketId.ToString() }]);
        status.Should().Be(nameof(SupportTicketStatus.Resolved));
    }

    [Fact]
    public async Task GetTicketDetail_WhenInternalNoteExists_ShouldFilterItOutInResponse()
    {
        // Arrange
        var ticketId = await CreateTicketViaApi();
        SeedInternalNote(ticketId, "Investigating with infra team. Do not share.");
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/api/account/support-tickets/{ticketId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<TicketDetailResponse>();
        payload.Should().NotBeNull();
        payload.Messages.Should().OnlyContain(m => m.AuthorKind != SupportMessageAuthorKind.Internal);
        payload.Messages.Should().NotContain(m => m.Body.Contains("Investigating with infra team"));
    }

    [Fact]
    public async Task MarkResolvedByUser_WhenCalledOnAwaitingTicket_ShouldTransitionToResolved()
    {
        // Arrange
        var ticketId = await CreateTicketViaApi();
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsync($"/api/account/support-tickets/{ticketId}/mark-resolved", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = Connection.ExecuteScalar<string>("SELECT status FROM support_tickets WHERE id = @id", [new { id = ticketId.ToString() }]);
        status.Should().Be(nameof(SupportTicketStatus.Resolved));
    }

    [Fact]
    public async Task CloseTicketByUser_WhenSubmittedWithCsat_ShouldPersistCsatAndKeepTicketResolved()
    {
        // Arrange
        var ticketId = await CreateTicketViaApi();
        SetTicketStatus(ticketId, SupportTicketStatus.Resolved);
        TelemetryEventsCollectorSpy.Reset();
        var command = new CloseTicketByUserCommand { CsatScore = SupportTicketCsatScore.Helpful, CsatComment = "Quick and clear." };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync($"/api/account/support-tickets/{ticketId}/close", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var csatJson = Connection.ExecuteScalar<string>("SELECT csat FROM support_tickets WHERE id = @id", [new { id = ticketId.ToString() }]);
        csatJson.Should().NotBeNullOrEmpty();
        csatJson.Should().Contain("Helpful");
        // Resolved is the terminal status going forward; the legacy Closed value is no longer written.
        var status = Connection.ExecuteScalar<string>("SELECT status FROM support_tickets WHERE id = @id", [new { id = ticketId.ToString() }]);
        status.Should().Be(nameof(SupportTicketStatus.Resolved));
        TelemetryEventsCollectorSpy.CollectedEvents.Should().Contain(e => e.GetType().Name == "SupportTicketCsatSubmitted");
    }

    [Fact]
    public async Task CloseTicketByUser_WhenCsatSubmittedAfterAlreadyResolved_ShouldNotReemitClosedEvent()
    {
        // Arrange
        var ticketId = await CreateTicketViaApi();
        var markResolvedResponse = await AuthenticatedOwnerHttpClient.PostAsync($"/api/account/support-tickets/{ticketId}/mark-resolved", null);
        markResolvedResponse.EnsureSuccessStatusCode();
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync(
            $"/api/account/support-tickets/{ticketId}/close",
            new CloseTicketByUserCommand { CsatScore = SupportTicketCsatScore.Helpful, CsatComment = "Thanks." }
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        TelemetryEventsCollectorSpy.CollectedEvents.Count(e => e.GetType().Name == "SupportTicketClosed").Should().Be(0);
        TelemetryEventsCollectorSpy.CollectedEvents.Should().Contain(e => e.GetType().Name == "SupportTicketCsatSubmitted");
    }

    [Fact]
    public async Task CloseTicketByUser_WhenCsatReplayedOnAlreadyRatedTicket_ShouldReturnBadRequest()
    {
        // Arrange
        var ticketId = await CreateTicketViaApi();
        var markResolvedResponse = await AuthenticatedOwnerHttpClient.PostAsync($"/api/account/support-tickets/{ticketId}/mark-resolved", null);
        markResolvedResponse.EnsureSuccessStatusCode();
        var firstCloseResponse = await AuthenticatedOwnerHttpClient.PostAsJsonAsync(
            $"/api/account/support-tickets/{ticketId}/close",
            new CloseTicketByUserCommand { CsatScore = SupportTicketCsatScore.Helpful, CsatComment = "Quick and clear." }
        );
        firstCloseResponse.EnsureSuccessStatusCode();
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync(
            $"/api/account/support-tickets/{ticketId}/close",
            new CloseTicketByUserCommand { CsatScore = SupportTicketCsatScore.NotGreat, CsatComment = "Changed my mind." }
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var csatJson = Connection.ExecuteScalar<string>("SELECT csat FROM support_tickets WHERE id = @id", [new { id = ticketId.ToString() }]);
        csatJson.Should().Contain("Helpful");
        csatJson.Should().NotContain("NotGreat");
    }

    [Fact]
    public async Task ReopenTicket_WhenClosedWithCsatInsideReopenWindow_ShouldTransitionToAwaitingAgentAndPreserveCsat()
    {
        // Arrange. Close goes through the API so the aggregate sets ResolvedAt, which is what
        // CanBeReopenedAt depends on. The reopen window is 7 days, so a just-resolved ticket is
        // eligible.
        var ticketId = await CreateTicketViaApi();
        var markResolvedResponse = await AuthenticatedOwnerHttpClient.PostAsync($"/api/account/support-tickets/{ticketId}/mark-resolved", null);
        markResolvedResponse.EnsureSuccessStatusCode();
        var closeCommand = new CloseTicketByUserCommand { CsatScore = SupportTicketCsatScore.Ok };
        var closeResponse = await AuthenticatedOwnerHttpClient.PostAsJsonAsync($"/api/account/support-tickets/{ticketId}/close", closeCommand);
        closeResponse.EnsureSuccessStatusCode();
        TelemetryEventsCollectorSpy.Reset();

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsync($"/api/account/support-tickets/{ticketId}/reopen", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = Connection.ExecuteScalar<string>("SELECT status FROM support_tickets WHERE id = @id", [new { id = ticketId.ToString() }]);
        status.Should().Be(nameof(SupportTicketStatus.AwaitingAgent));
        var csatJson = Connection.ExecuteScalar<string>("SELECT csat FROM support_tickets WHERE id = @id", [new { id = ticketId.ToString() }]);
        csatJson.Should().Contain("Ok");
    }

    [Fact]
    public async Task ReopenTicket_WhenResolvedWithinSevenDays_ShouldTransitionToAwaitingAgentAndClearResolvedAt()
    {
        // Arrange. Resolved 6 days ago is inside the 7-day reopen window.
        var ticketId = await CreateTicketViaApi();
        SetTicketStatus(ticketId, SupportTicketStatus.Resolved);
        SetResolvedAt(ticketId, DateTimeOffset.UtcNow.AddDays(-6));

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsync($"/api/account/support-tickets/{ticketId}/reopen", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var status = Connection.ExecuteScalar<string>("SELECT status FROM support_tickets WHERE id = @id", [new { id = ticketId.ToString() }]);
        status.Should().Be(nameof(SupportTicketStatus.AwaitingAgent));
        var resolvedAt = Connection.ExecuteScalar<DateTimeOffset?>("SELECT resolved_at FROM support_tickets WHERE id = @id", [new { id = ticketId.ToString() }]);
        resolvedAt.Should().BeNull();
    }

    [Fact]
    public async Task ReopenTicket_WhenResolvedPastSevenDays_ShouldReturnBadRequest()
    {
        // Arrange. Resolved 8 days ago is past the 7-day reopen window.
        var ticketId = await CreateTicketViaApi();
        SetTicketStatus(ticketId, SupportTicketStatus.Resolved);
        SetResolvedAt(ticketId, DateTimeOffset.UtcNow.AddDays(-8));

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsync($"/api/account/support-tickets/{ticketId}/reopen", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var status = Connection.ExecuteScalar<string>("SELECT status FROM support_tickets WHERE id = @id", [new { id = ticketId.ToString() }]);
        status.Should().Be(nameof(SupportTicketStatus.Resolved));
    }

    [Fact]
    public async Task SubmitCsat_WhenTicketIsActive_ShouldReturnBadRequest()
    {
        // Arrange. A ticket that has not reached a terminal status should not accept a rating.
        var ticketId = await CreateTicketViaApi();
        SetTicketStatus(ticketId, SupportTicketStatus.AwaitingAgent);
        var rateCommand = new SubmitCsatCommand(SupportTicketCsatScore.Helpful, null);

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync($"/api/account/support-tickets/{ticketId}/csat", rateCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var status = Connection.ExecuteScalar<string>("SELECT status FROM support_tickets WHERE id = @id", [new { id = ticketId.ToString() }]);
        status.Should().Be(nameof(SupportTicketStatus.AwaitingAgent));
        var csatJson = Connection.ExecuteScalar<string>("SELECT csat FROM support_tickets WHERE id = @id", [new { id = ticketId.ToString() }]);
        csatJson.Should().BeNull();
    }

    [Fact]
    public async Task SubmitCsat_WhenRatingFreshAndNotStale_ShouldReturnBadRequest()
    {
        // Arrange. Record a CSAT via close, then try to submit again immediately (no reopen in between).
        var ticketId = await CreateTicketViaApi();
        var markResolvedResponse = await AuthenticatedOwnerHttpClient.PostAsync($"/api/account/support-tickets/{ticketId}/mark-resolved", null);
        markResolvedResponse.EnsureSuccessStatusCode();
        var closeCommand = new CloseTicketByUserCommand { CsatScore = SupportTicketCsatScore.Helpful, CsatComment = "Quick fix" };
        var closeResponse = await AuthenticatedOwnerHttpClient.PostAsJsonAsync($"/api/account/support-tickets/{ticketId}/close", closeCommand);
        closeResponse.EnsureSuccessStatusCode();
        var rerateCommand = new SubmitCsatCommand(SupportTicketCsatScore.NotGreat, "Changed my mind");

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync($"/api/account/support-tickets/{ticketId}/csat", rerateCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var csatJson = Connection.ExecuteScalar<string>("SELECT csat FROM support_tickets WHERE id = @id", [new { id = ticketId.ToString() }]);
        csatJson.Should().Contain("Helpful");
        csatJson.Should().NotContain("NotGreat");
    }

    [Fact]
    public async Task SubmitCsat_WhenRatingStaleAfterReopen_ShouldOverwriteAndSucceed()
    {
        // Arrange. Close + rate, reopen (Status to AwaitingAgent), re-resolve so the ticket is back
        // in a terminal status, then submit a fresh rating. The reopen makes the prior CSAT stale.
        var ticketId = await CreateTicketViaApi();
        var markResolvedResponse = await AuthenticatedOwnerHttpClient.PostAsync($"/api/account/support-tickets/{ticketId}/mark-resolved", null);
        markResolvedResponse.EnsureSuccessStatusCode();
        var closeCommand = new CloseTicketByUserCommand { CsatScore = SupportTicketCsatScore.Helpful, CsatComment = "Initial rating" };
        var closeResponse = await AuthenticatedOwnerHttpClient.PostAsJsonAsync($"/api/account/support-tickets/{ticketId}/close", closeCommand);
        closeResponse.EnsureSuccessStatusCode();
        var reopenResponse = await AuthenticatedOwnerHttpClient.PostAsync($"/api/account/support-tickets/{ticketId}/reopen", null);
        reopenResponse.EnsureSuccessStatusCode();
        var reMarkResolvedResponse = await AuthenticatedOwnerHttpClient.PostAsync($"/api/account/support-tickets/{ticketId}/mark-resolved", null);
        reMarkResolvedResponse.EnsureSuccessStatusCode();
        var rerateCommand = new SubmitCsatCommand(SupportTicketCsatScore.NotGreat, "Actually broken again");

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsJsonAsync($"/api/account/support-tickets/{ticketId}/csat", rerateCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var csatJson = Connection.ExecuteScalar<string>("SELECT csat FROM support_tickets WHERE id = @id", [new { id = ticketId.ToString() }]);
        csatJson.Should().Contain("NotGreat");
        csatJson.Should().Contain("Actually broken again");
    }

    [Fact]
    public async Task GetMyTickets_WhenCalled_ShouldOnlyReturnReportersOwnTickets()
    {
        // Arrange
        await CreateTicketViaApi(AuthenticatedOwnerHttpClient);
        await CreateTicketViaApi(AuthenticatedMemberHttpClient);

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account/support-tickets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<MyTicketsResponse>();
        payload.Should().NotBeNull();
        payload.Active.Should().OnlyContain(t => t.ShortDisplayId.Length == 6);
        var rowsForOwner = Connection.ExecuteScalar<long>(
            "SELECT COUNT(*) FROM support_tickets WHERE reporter_id = @reporterId",
            [new { reporterId = DatabaseSeeder.Tenant1Owner.Id.ToString() }]
        );
        payload.Active.Length.Should().Be((int)rowsForOwner);
    }

    [Fact]
    public async Task GetMyTickets_WhenResolvedPastReopenWindow_ShouldBucketAsClosedAndReportComputedStatus()
    {
        // Arrange. Resolved 8 days ago is past the 7-day reopen window, so the computed display
        // status promotes the row from Resolved to Closed even though the DB still says Resolved.
        var ticketId = await CreateTicketViaApi();
        SetTicketStatus(ticketId, SupportTicketStatus.Resolved);
        SetResolvedAt(ticketId, DateTimeOffset.UtcNow.AddDays(-8));

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account/support-tickets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<MyTicketsResponse>();
        payload.Should().NotBeNull();
        payload.Closed.Should().Contain(t => t.Id == ticketId && t.Status == SupportTicketStatus.Closed);
        payload.Active.Should().NotContain(t => t.Id == ticketId);
    }

    [Fact]
    public async Task GetMyTickets_WhenResolvedInsideReopenWindowAndUnrated_ShouldBucketAsActive()
    {
        // Arrange. Resolved 2 days ago without a CSAT rating is inside the 7-day reopen window, so
        // the reporter still sees the ticket in the Active bucket with the Reopen affordance.
        var ticketId = await CreateTicketViaApi();
        SetTicketStatus(ticketId, SupportTicketStatus.Resolved);
        SetResolvedAt(ticketId, DateTimeOffset.UtcNow.AddDays(-2));

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account/support-tickets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<MyTicketsResponse>();
        payload.Should().NotBeNull();
        payload.Active.Should().Contain(t => t.Id == ticketId && t.Status == SupportTicketStatus.Resolved);
        payload.Closed.Should().NotContain(t => t.Id == ticketId);
    }

    [Fact]
    public async Task GetMyTickets_WhenLegacyClosedRow_ShouldBucketAsClosedAndReportRawStatus()
    {
        // Arrange. Legacy rows from before the Resolved-is-terminal switch carry Status=Closed and
        // must continue to bucket as Closed without depending on ResolvedAt.
        var ticketId = await CreateTicketViaApi();
        SetTicketStatus(ticketId, SupportTicketStatus.Closed);

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account/support-tickets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<MyTicketsResponse>();
        payload.Should().NotBeNull();
        payload.Closed.Should().Contain(t => t.Id == ticketId && t.Status == SupportTicketStatus.Closed);
        payload.Active.Should().NotContain(t => t.Id == ticketId);
    }

    [Fact]
    public async Task GetMyTickets_WhenResolvedWithFreshCsat_ShouldBucketAsClosedAndReportComputedStatus()
    {
        // Arrange. A CSAT submission is the reporter signing off, so the ticket is promoted to
        // Closed for bucketing even though the reopen window is still open and the row is Resolved.
        var ticketId = await CreateTicketViaApi();
        var markResolvedResponse = await AuthenticatedOwnerHttpClient.PostAsync($"/api/account/support-tickets/{ticketId}/mark-resolved", null);
        markResolvedResponse.EnsureSuccessStatusCode();
        var closeCommand = new CloseTicketByUserCommand { CsatScore = SupportTicketCsatScore.Helpful, CsatComment = "Done." };
        var closeResponse = await AuthenticatedOwnerHttpClient.PostAsJsonAsync($"/api/account/support-tickets/{ticketId}/close", closeCommand);
        closeResponse.EnsureSuccessStatusCode();

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync("/api/account/support-tickets");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<MyTicketsResponse>();
        payload.Should().NotBeNull();
        payload.Closed.Should().Contain(t => t.Id == ticketId && t.Status == SupportTicketStatus.Closed);
        payload.Active.Should().NotContain(t => t.Id == ticketId);
    }

    [Fact]
    public async Task GetTicketDetail_WhenResolvedAndReopened_ShouldExposeUserVisibleHistoryEventsInOrder()
    {
        // Arrange. Resolve then reopen so the ticket has one Resolved event followed by one Reopened
        // event. The response should expose only those two as user-visible history events; other
        // history entries (Created, MessagePosted) must stay filtered out of the end-user surface.
        var ticketId = await CreateTicketViaApi();
        var markResolvedResponse = await AuthenticatedOwnerHttpClient.PostAsync($"/api/account/support-tickets/{ticketId}/mark-resolved", null);
        markResolvedResponse.EnsureSuccessStatusCode();
        var reopenResponse = await AuthenticatedOwnerHttpClient.PostAsync($"/api/account/support-tickets/{ticketId}/reopen", null);
        reopenResponse.EnsureSuccessStatusCode();

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/api/account/support-tickets/{ticketId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<TicketDetailResponse>();
        payload.Should().NotBeNull();
        payload.HistoryEvents.Should().HaveCount(2);
        payload.HistoryEvents[0].Type.Should().Be(TicketUserVisibleEventType.Resolved);
        payload.HistoryEvents[0].ActorKind.Should().Be(SupportMessageAuthorKind.User);
        payload.HistoryEvents[1].Type.Should().Be(TicketUserVisibleEventType.Reopened);
        payload.HistoryEvents[1].ActorKind.Should().Be(SupportMessageAuthorKind.User);
        payload.HistoryEvents[1].OccurredAt.Should().BeOnOrAfter(payload.HistoryEvents[0].OccurredAt);
    }

    [Fact]
    public async Task GetTicketDetail_WhenTicketBelongsToAnotherTenant_ShouldReturnNotFound()
    {
        // Arrange
        var otherTenantId = SeedOtherTenant();
        var otherTicketId = SeedOtherTenantTicket(otherTenantId);

        // Act
        var response = await AuthenticatedOwnerHttpClient.GetAsync($"/api/account/support-tickets/{otherTicketId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReplyToTicketAsUser_WhenTicketBelongsToAnotherTenant_ShouldReturnNotFound()
    {
        // Arrange
        var otherTenantId = SeedOtherTenant();
        var otherTicketId = SeedOtherTenantTicket(otherTenantId);
        var form = new MultipartFormDataContent
        {
            { new StringContent("Cross-tenant reply attempt. Should be invisible."), "body" },
            { new StringContent("false"), "markAsResolved" }
        };

        // Act
        var response = await AuthenticatedOwnerHttpClient.PostAsync($"/api/account/support-tickets/{otherTicketId}/reply", form);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private long SeedOtherTenant()
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
        return otherTenantId;
    }

    private SupportTicketId SeedOtherTenantTicket(long otherTenantId)
    {
        var id = SupportTicketId.NewId();
        var now = DateTimeOffset.UtcNow;
        Connection.Insert("support_tickets", [
                ("tenant_id", otherTenantId),
                ("id", id.ToString()),
                ("created_at", now.AddMinutes(-10)),
                ("modified_at", null),
                ("reporter_id", UserId.NewId().ToString()),
                ("reporter_role_snapshot", "Owner"),
                ("reporter_email_snapshot", "other@tenant.example"),
                ("subject", "Other tenant's ticket"),
                ("category", nameof(SupportTicketCategory.Other)),
                ("status", nameof(SupportTicketStatus.New)),
                ("assignee", null),
                ("last_activity_at", now),
                ("resolved_at", null),
                ("closed_at", null),
                ("csat", null),
                ("messages", "[]"),
                ("history_events", "[]")
            ]
        );
        return id;
    }

    private async Task<SupportTicketId> CreateTicketViaApi(HttpClient? client = null)
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent("Subject for lifecycle test"), "subject" },
            { new StringContent("Body for the lifecycle test that exceeds the minimum length."), "body" },
            { new StringContent(nameof(SupportTicketCategory.Account)), "category" }
        };
        var response = await (client ?? AuthenticatedOwnerHttpClient).PostAsync("/api/account/support-tickets", form);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SupportTicketId>())!;
    }

    private void SetTicketStatus(SupportTicketId ticketId, SupportTicketStatus status)
    {
        Connection.Update("support_tickets", "id", ticketId.ToString(), [("status", status.ToString())]);
    }

    private void SetResolvedAt(SupportTicketId ticketId, DateTimeOffset resolvedAt)
    {
        Connection.Update("support_tickets", "id", ticketId.ToString(), [("resolved_at", resolvedAt)]);
    }

    private void SeedInternalNote(SupportTicketId ticketId, string body)
    {
        var note = new SupportMessage(SupportMessageId.NewId(), "staff-oid", SupportMessageAuthorKind.Internal, "Support Staff", body, [], DateTimeOffset.UtcNow);
        var existingJson = Connection.ExecuteScalar<string>("SELECT messages FROM support_tickets WHERE id = @id", [new { id = ticketId.ToString() }]);
        var existing = JsonSerializer.Deserialize<SupportMessage[]>(existingJson) ?? [];
        var combined = existing.Append(note).ToArray();
        Connection.Update("support_tickets", "id", ticketId.ToString(), [("messages", JsonSerializer.Serialize(combined))]);
    }
}
