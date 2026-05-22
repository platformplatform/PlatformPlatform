using System.Collections.Immutable;
using JetBrains.Annotations;
using SharedKernel.Domain;
using SharedKernel.StronglyTypedIds;

namespace Account.Features.SupportTickets.Domain;

public sealed class SupportTicket : AggregateRoot<SupportTicketId>, ITenantScopedEntity
{
    public const int SubjectMinLength = 4;
    public const int SubjectMaxLength = 200;
    public const int MessageBodyMinLength = 1;
    public const int MessageBodyMaxLength = 10_000;
    public const int CsatCommentMaxLength = 2_000;
    public const int ShortDisplayIdLength = 6;

    // A Resolved ticket auto-locks for further reopen after this window. The reporter has had a
    // week to come back and re-engage if the answer didn't actually solve their problem, and after
    // that the ticket is treated as Closed. We don't have background jobs to flip the row's status,
    // so the rule lives on the aggregate and the DB stays Resolved + ResolvedAt forever.
    public static readonly TimeSpan ReopenWindowAfterResolved = TimeSpan.FromDays(7);

    private SupportTicket(TenantId tenantId, UserId reporterId, string reporterRoleSnapshot, string reporterEmailSnapshot, string subject, SupportTicketCategory category, DateTimeOffset lastActivityAt)
        : base(SupportTicketId.NewId())
    {
        TenantId = tenantId;
        ReporterId = reporterId;
        ReporterRoleSnapshot = reporterRoleSnapshot;
        ReporterEmailSnapshot = reporterEmailSnapshot;
        Subject = subject;
        Category = category;
        Status = SupportTicketStatus.New;
        LastActivityAt = lastActivityAt;
        Messages = [];
        HistoryEvents = [];
    }

    // Derived from the trailing ULID characters of Id. ULIDs are Crockford Base32 (uppercase A-Z and
    // digits, excluding I/L/O/U), giving 32^6 ~= 1 billion permutations. Collisions within a tenant
    // are statistically negligible without needing a separate uniqueness probe.
    public string ShortDisplayId => Id.Value[^ShortDisplayIdLength..];

    public UserId ReporterId { get; private set; }

    public string ReporterRoleSnapshot { get; private set; }

    [UsedImplicitly]
    public string ReporterEmailSnapshot { get; private set; }

    public string Subject { get; private set; }

    public SupportTicketCategory Category { get; private set; }

    public SupportTicketStatus Status { get; private set; }

    public BackOfficeStaffRef? Assignee { get; private set; }

    public DateTimeOffset LastActivityAt { get; private set; }

    public DateTimeOffset? ResolvedAt { get; private set; }

    public DateTimeOffset? ClosedAt { get; private set; }

    public SupportTicketCsat? Csat { get; private set; }

    public ImmutableArray<SupportMessage> Messages { get; private set; }

    public ImmutableArray<SupportTicketHistoryEvent> HistoryEvents { get; private set; }

    public TenantId TenantId { get; }

    public static SupportTicket Create(TenantId tenantId, UserId reporterId, string reporterRoleSnapshot, string reporterEmailSnapshot, string subject, SupportTicketCategory category, DateTimeOffset now)
    {
        var ticket = new SupportTicket(tenantId, reporterId, reporterRoleSnapshot, reporterEmailSnapshot, subject, category, now);
        ticket.HistoryEvents = ticket.HistoryEvents.Add(
            SupportTicketHistoryEvent.Create(SupportTicketHistoryEventType.Created, SupportMessageAuthorKind.User, reporterEmailSnapshot, now)
        );
        return ticket;
    }

    public SupportMessage PostUserMessage(UserId authorUserId, string body, ImmutableArray<SupportMessageAttachment> attachments, DateTimeOffset now)
    {
        // Terminal tickets must be explicitly reopened by the handler before a new message lands.
        // Without this guard a reply silently bypasses the 7-day reopen window, skips the Reopened
        // history event, and locks the reporter out of re-rating CSAT after the next re-resolve.
        if (Status is SupportTicketStatus.Resolved or SupportTicketStatus.Closed)
        {
            throw new InvalidOperationException("Cannot post a user message on a terminal ticket; call ReopenByUser first.");
        }

        var message = SupportMessage.Create(authorUserId.Value, SupportMessageAuthorKind.User, ReporterEmailSnapshot, body, attachments, now);
        Messages = Messages.Add(message);
        LastActivityAt = now;
        HistoryEvents = HistoryEvents.Add(
            SupportTicketHistoryEvent.Create(SupportTicketHistoryEventType.MessagePosted, SupportMessageAuthorKind.User, ReporterEmailSnapshot, now, attachments.Length > 0)
        );

        // A user message always transitions to AwaitingAgent.
        ApplyStatusTransition(SupportTicketStatus.AwaitingAgent, SupportMessageAuthorKind.User, ReporterEmailSnapshot, now, false);
        return message;
    }

    public SupportMessage PostStaffPublicMessage(BackOfficeStaffRef staff, string body, ImmutableArray<SupportMessageAttachment> attachments, DateTimeOffset now)
    {
        // Terminal tickets must be explicitly reopened by the handler before a staff reply lands.
        // See PostUserMessage for the matching invariant on the user side.
        if (Status is SupportTicketStatus.Resolved or SupportTicketStatus.Closed)
        {
            throw new InvalidOperationException("Cannot post a staff message on a terminal ticket; call ReopenByStaff first.");
        }

        var message = SupportMessage.Create(staff.ObjectId, SupportMessageAuthorKind.Staff, staff.DisplayName, body, attachments, now);
        Messages = Messages.Add(message);
        LastActivityAt = now;
        HistoryEvents = HistoryEvents.Add(
            SupportTicketHistoryEvent.Create(SupportTicketHistoryEventType.MessagePosted, SupportMessageAuthorKind.Staff, staff.DisplayName, now, attachments.Length > 0)
        );

        ApplyStatusTransition(SupportTicketStatus.AwaitingUser, SupportMessageAuthorKind.Staff, staff.DisplayName, now, false);
        return message;
    }

    public SupportMessage PostStaffInternalNote(BackOfficeStaffRef staff, string body, ImmutableArray<SupportMessageAttachment> attachments, DateTimeOffset now)
    {
        var message = SupportMessage.Create(staff.ObjectId, SupportMessageAuthorKind.Internal, staff.DisplayName, body, attachments, now);
        Messages = Messages.Add(message);
        LastActivityAt = now;
        HistoryEvents = HistoryEvents.Add(
            SupportTicketHistoryEvent.Create(SupportTicketHistoryEventType.MessagePosted, SupportMessageAuthorKind.Internal, staff.DisplayName, now, attachments.Length > 0)
        );
        return message;
    }

    public bool MarkResolvedByUser(DateTimeOffset now)
    {
        if (Status is SupportTicketStatus.Resolved or SupportTicketStatus.Closed) return false;
        ApplyStatusTransition(SupportTicketStatus.Resolved, SupportMessageAuthorKind.User, ReporterEmailSnapshot, now, true);
        return true;
    }

    public bool MarkResolvedByStaff(BackOfficeStaffRef staff, DateTimeOffset now)
    {
        if (Status is SupportTicketStatus.Resolved or SupportTicketStatus.Closed) return false;
        ApplyStatusTransition(SupportTicketStatus.Resolved, SupportMessageAuthorKind.Staff, staff.DisplayName, now, true);
        return true;
    }

    public bool CloseByUser(DateTimeOffset now)
    {
        // The end-user "Close" action is the user-facing label for marking a ticket Resolved. Resolved
        // is the terminal status; the 7-day reopen window then derives the UI's "Closed" state without
        // a second DB transition. The legacy Closed enum value is preserved for historical rows only.
        if (Status is SupportTicketStatus.Resolved or SupportTicketStatus.Closed) return false;
        ApplyStatusTransition(SupportTicketStatus.Resolved, SupportMessageAuthorKind.User, ReporterEmailSnapshot, now, true);
        return true;
    }

    // A CSAT rating is "stale" when the ticket has been re-resolved since the rating was submitted.
    // After a reopen and re-resolve cycle the user should be able to submit a fresh rating instead
    // of being locked out by the old one. Derives from ResolvedAt so the invariant holds even when
    // a status path transitions out of Resolved without emitting a Reopened history event.
    public bool IsCsatStale()
    {
        if (Csat is null) return false;
        return ResolvedAt is { } resolvedAt && resolvedAt > Csat.SubmittedAt;
    }

    // A ticket is reopenable when it is Closed OR when it is Resolved within the reopen window.
    // Used by the API to gate the reopen endpoint AND by query handlers to tell the UI whether to
    // show the Reopen button.
    public bool CanBeReopenedAt(DateTimeOffset now)
    {
        if (Status is SupportTicketStatus.Closed) return true;
        if (Status is SupportTicketStatus.Resolved && ResolvedAt is { } resolvedAt)
        {
            return now - resolvedAt <= ReopenWindowAfterResolved;
        }

        return false;
    }

    // The display status the UI should render and filter on. We never write Closed to the row going
    // forward, so a Resolved ticket is promoted to Closed when the reporter has signed off with a
    // fresh CSAT rating or when the 7-day reopen window has elapsed. Legacy Closed rows pass
    // through. All other statuses pass through unchanged. A stale CSAT (from a prior resolve cycle
    // that was later reopened) does NOT count - the reporter is invited to re-rate and the ticket
    // should appear Resolved until they do or the window closes.
    public SupportTicketStatus ComputeDisplayStatus(DateTimeOffset now)
    {
        if (Status is SupportTicketStatus.Closed) return SupportTicketStatus.Closed;
        if (Status is not SupportTicketStatus.Resolved) return Status;
        if (Csat is not null && !IsCsatStale()) return SupportTicketStatus.Closed;
        if (ResolvedAt is { } resolvedAt && now - resolvedAt > ReopenWindowAfterResolved) return SupportTicketStatus.Closed;
        return SupportTicketStatus.Resolved;
    }

    public bool ReopenByUser(DateTimeOffset now)
    {
        if (!CanBeReopenedAt(now)) return false;
        Status = SupportTicketStatus.AwaitingAgent;
        ClosedAt = null;
        ResolvedAt = null;
        LastActivityAt = now;
        HistoryEvents = HistoryEvents.Add(
            SupportTicketHistoryEvent.Create(SupportTicketHistoryEventType.Reopened, SupportMessageAuthorKind.User, ReporterEmailSnapshot, now)
        );
        return true;
    }

    public bool ReopenByStaff(BackOfficeStaffRef staff, DateTimeOffset now)
    {
        if (!CanBeReopenedAt(now)) return false;
        Status = SupportTicketStatus.AwaitingAgent;
        ClosedAt = null;
        ResolvedAt = null;
        LastActivityAt = now;
        HistoryEvents = HistoryEvents.Add(
            SupportTicketHistoryEvent.Create(SupportTicketHistoryEventType.Reopened, SupportMessageAuthorKind.Staff, staff.DisplayName, now)
        );
        return true;
    }

    public bool Assign(BackOfficeStaffRef? assignee, BackOfficeStaffRef actor, DateTimeOffset now)
    {
        if (Equals(Assignee, assignee)) return false;
        Assignee = assignee;
        LastActivityAt = now;
        HistoryEvents = HistoryEvents.Add(
            SupportTicketHistoryEvent.Create(SupportTicketHistoryEventType.AssigneeChanged, SupportMessageAuthorKind.Staff, actor.DisplayName, now, payload: assignee?.DisplayName)
        );
        return true;
    }

    public void SubmitCsat(SupportTicketCsatScore score, string? comment, DateTimeOffset now)
    {
        // Submitting CSAT only records the rating; status transitions are owned by the user's
        // explicit Close or Mark-resolved actions. The handler guards against CSAT on active tickets.
        Csat = new SupportTicketCsat(score, comment, now);
        LastActivityAt = now;
        HistoryEvents = HistoryEvents.Add(
            SupportTicketHistoryEvent.Create(SupportTicketHistoryEventType.CsatSubmitted, SupportMessageAuthorKind.User, ReporterEmailSnapshot, now, payload: score.ToString())
        );
    }

    private void ApplyStatusTransition(SupportTicketStatus newStatus, SupportMessageAuthorKind actorKind, string actorDisplayName, DateTimeOffset now, bool recordHistory)
    {
        if (Status == newStatus) return;

        // Leaving Closed always clears ClosedAt; the CSAT record is preserved (see SubmitCsat).
        if (Status is SupportTicketStatus.Closed) ClosedAt = null;
        // Leaving Resolved clears ResolvedAt; entering Resolved sets it.
        if (Status is SupportTicketStatus.Resolved) ResolvedAt = null;

        Status = newStatus;
        if (newStatus is SupportTicketStatus.Resolved) ResolvedAt = now;

        LastActivityAt = now;

        if (recordHistory)
        {
            HistoryEvents = HistoryEvents.Add(
                SupportTicketHistoryEvent.Create(SupportTicketHistoryEventType.StatusChanged, actorKind, actorDisplayName, now, payload: newStatus.ToString())
            );
        }
    }
}

[PublicAPI]
[IdPrefix("tkt")]
[JsonConverter(typeof(StronglyTypedIdJsonConverter<string, SupportTicketId>))]
public sealed record SupportTicketId(string Value) : StronglyTypedUlid<SupportTicketId>(Value)
{
    public override string ToString()
    {
        return Value;
    }
}

[PublicAPI]
[IdPrefix("tmsg")]
[JsonConverter(typeof(StronglyTypedIdJsonConverter<string, SupportMessageId>))]
public sealed record SupportMessageId(string Value) : StronglyTypedUlid<SupportMessageId>(Value)
{
    public override string ToString()
    {
        return Value;
    }
}

[PublicAPI]
public sealed record SupportTicketCsat(SupportTicketCsatScore Score, string? Comment, DateTimeOffset SubmittedAt);

// BackOfficeStaffRef captures the Entra ID identity of a back-office staff user at the time they
// touched the ticket. ObjectId is the Entra oid claim. DisplayName is the friendly name claim.
[PublicAPI]
public sealed record BackOfficeStaffRef(string ObjectId, string DisplayName);

[PublicAPI]
public sealed record SupportMessage(
    SupportMessageId Id,
    string AuthorIdentityValue,
    SupportMessageAuthorKind AuthorKind,
    string AuthorDisplayName,
    string Body,
    ImmutableArray<SupportMessageAttachment> Attachments,
    DateTimeOffset PostedAt
)
{
    internal static SupportMessage Create(string authorIdentityValue, SupportMessageAuthorKind authorKind, string authorDisplayName, string body, ImmutableArray<SupportMessageAttachment> attachments, DateTimeOffset postedAt)
    {
        return new SupportMessage(SupportMessageId.NewId(), authorIdentityValue, authorKind, authorDisplayName, body, attachments, postedAt);
    }
}

[PublicAPI]
public sealed record SupportMessageAttachment(string FileName, string ContentType, long SizeInBytes, string BlobUrl);

[PublicAPI]
public sealed record SupportTicketHistoryEvent(
    SupportTicketHistoryEventType Type,
    SupportMessageAuthorKind ActorKind,
    string ActorDisplayName,
    DateTimeOffset OccurredAt,
    bool HasAttachment,
    string? Payload
)
{
    public static SupportTicketHistoryEvent Create(
        SupportTicketHistoryEventType type,
        SupportMessageAuthorKind actorKind,
        string actorDisplayName,
        DateTimeOffset now,
        bool hasAttachment = false,
        string? payload = null
    )
    {
        return new SupportTicketHistoryEvent(type, actorKind, actorDisplayName, now, hasAttachment, payload);
    }
}
