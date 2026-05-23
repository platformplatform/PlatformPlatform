using JetBrains.Annotations;

namespace Account.Features.SupportTickets.Domain;

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SupportTicketCategory
{
    Billing,
    Account,
    HowTo,
    Bug,
    Feature,
    Feedback,
    Other
}

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SupportTicketStatus
{
    New,
    AwaitingAgent,
    AwaitingUser,
    AwaitingInternal,
    Resolved,
    Closed
}

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SupportMessageAuthorKind
{
    User,
    Staff,
    Internal
}

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SupportTicketCsatScore
{
    Helpful,
    Ok,
    NotGreat
}

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SupportTicketHistoryEventType
{
    Created,
    MessagePosted,
    StatusChanged,
    AssigneeChanged,
    CsatSubmitted,
    Reopened
}

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SupportTicketAssigneeFilter
{
    Any,
    Unassigned,
    Me
}
