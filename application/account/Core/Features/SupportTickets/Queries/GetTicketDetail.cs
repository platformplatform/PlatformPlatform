using Account.Features.SupportTickets.Domain;
using Account.Features.SupportTickets.Shared;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.ExecutionContext;

namespace Account.Features.SupportTickets.Queries;

[PublicAPI]
public sealed record GetTicketDetailQuery(SupportTicketId Id) : IRequest<Result<TicketDetailResponse>>;

[PublicAPI]
public sealed record TicketDetailResponse(
    SupportTicketId Id,
    string ShortDisplayId,
    string Subject,
    SupportTicketCategory Category,
    SupportTicketStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastActivityAt,
    DateTimeOffset? ResolvedAt,
    DateTimeOffset? ClosedAt,
    TicketMessageView[] Messages,
    TicketHistoryEventView[] HistoryEvents,
    TicketCsatView? Csat,
    bool CanBeReopened,
    bool IsCsatStale
);

[PublicAPI]
public sealed record TicketMessageView(
    SupportMessageId Id,
    SupportMessageAuthorKind AuthorKind,
    string AuthorDisplayName,
    string Body,
    DateTimeOffset CreatedAt,
    TicketAttachmentView[] Attachments
);

[PublicAPI]
public sealed record TicketAttachmentView(string FileName, string ContentType, long SizeInBytes, string Url);

[PublicAPI]
public sealed record TicketCsatView(SupportTicketCsatScore Score, string? Comment, DateTimeOffset SubmittedAt);

// The end-user surface only renders a subset of history events as inline chat-thread entries.
// MessagePosted, AssigneeChanged, CsatSubmitted, and Created are intentionally excluded because
// they're either redundant (messages render as bubbles) or staff-only signal (assignment).
[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TicketUserVisibleEventType
{
    Resolved,
    Reopened
}

[PublicAPI]
public sealed record TicketHistoryEventView(
    TicketUserVisibleEventType Type,
    SupportMessageAuthorKind ActorKind,
    string ActorDisplayName,
    DateTimeOffset OccurredAt
);

public sealed class GetTicketDetailHandler(ISupportTicketRepository ticketRepository, IExecutionContext executionContext, TimeProvider timeProvider)
    : IRequestHandler<GetTicketDetailQuery, Result<TicketDetailResponse>>
{
    public async Task<Result<TicketDetailResponse>> Handle(GetTicketDetailQuery query, CancellationToken cancellationToken)
    {
        var ticket = await ticketRepository.GetByIdAsync(query.Id, cancellationToken);
        if (ticket is null) return Result<TicketDetailResponse>.NotFound($"Support ticket with id '{query.Id}' not found.");

        if (ticket.ReporterId != executionContext.UserInfo.Id!) return Result<TicketDetailResponse>.NotFound($"Support ticket with id '{query.Id}' not found.");

        var messages = ticket.Messages
            .Where(m => m.AuthorKind != SupportMessageAuthorKind.Internal)
            .Select(m => new TicketMessageView(
                    m.Id,
                    m.AuthorKind,
                    m.AuthorDisplayName,
                    m.Body,
                    m.PostedAt,
                    m.Attachments.Select(a => new TicketAttachmentView(a.FileName, a.ContentType, a.SizeInBytes, SupportTicketAttachmentDownloader.BuildReporterDownloadUrl(ticket.Id, m.Id, a.BlobUrl))).ToArray()
                )
            )
            .ToArray();

        var historyEvents = ticket.HistoryEvents
            .Select(ProjectUserVisibleEvent)
            .Where(view => view is not null)
            .Select(view => view!)
            .ToArray();

        var csat = ticket.Csat is null ? null : new TicketCsatView(ticket.Csat.Score, ticket.Csat.Comment, ticket.Csat.SubmittedAt);
        var now = timeProvider.GetUtcNow();

        return new TicketDetailResponse(
            ticket.Id,
            ticket.ShortDisplayId,
            ticket.Subject,
            ticket.Category,
            ticket.ComputeDisplayStatus(now),
            ticket.CreatedAt,
            ticket.LastActivityAt,
            ticket.ResolvedAt,
            ticket.ClosedAt,
            messages,
            historyEvents,
            csat,
            ticket.CanBeReopenedAt(now),
            ticket.IsCsatStale()
        );
    }

    // Resolved transitions are recorded as StatusChanged history events with payload "Resolved"
    // (see SupportTicket.ApplyStatusTransition). Reopened is its own event type. Everything else
    // is filtered out so the reporter only sees lifecycle events that match the status pill changes.
    private static TicketHistoryEventView? ProjectUserVisibleEvent(SupportTicketHistoryEvent historyEvent)
    {
        if (historyEvent.Type is SupportTicketHistoryEventType.Reopened)
        {
            return new TicketHistoryEventView(TicketUserVisibleEventType.Reopened, historyEvent.ActorKind, historyEvent.ActorDisplayName, historyEvent.OccurredAt);
        }

        if (historyEvent.Type is SupportTicketHistoryEventType.StatusChanged && historyEvent.Payload == nameof(SupportTicketStatus.Resolved))
        {
            return new TicketHistoryEventView(TicketUserVisibleEventType.Resolved, historyEvent.ActorKind, historyEvent.ActorDisplayName, historyEvent.OccurredAt);
        }

        return null;
    }
}
