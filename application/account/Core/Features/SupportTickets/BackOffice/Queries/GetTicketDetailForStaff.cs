using Account.Features.Subscriptions.Domain;
using Account.Features.SupportTickets.Domain;
using Account.Features.SupportTickets.Queries;
using Account.Features.SupportTickets.Shared;
using Account.Features.Tenants.BackOffice.Queries;
using Account.Features.Tenants.Domain;
using Account.Features.Users.Domain;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.Domain;

namespace Account.Features.SupportTickets.BackOffice.Queries;

[PublicAPI]
public sealed record GetTicketDetailForStaffQuery(SupportTicketId Id) : IRequest<Result<StaffTicketDetailResponse>>;

[PublicAPI]
public sealed record StaffTicketDetailResponse(
    SupportTicketId Id,
    string ShortDisplayId,
    string Subject,
    SupportTicketCategory Category,
    SupportTicketStatus Status,
    StaffTicketReporter Reporter,
    StaffTicketAccount Account,
    StaffTicketAssignee? Assignee,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastActivityAt,
    DateTimeOffset? ResolvedAt,
    DateTimeOffset? ClosedAt,
    StaffTicketMessageView[] Messages,
    StaffTicketHistoryEventView[] HistoryEvents,
    TicketCsatView? Csat
);

[PublicAPI]
public sealed record StaffTicketReporter(
    UserId Id,
    string Email,
    string? FirstName,
    string? LastName,
    string RoleSnapshot,
    string? AvatarUrl,
    int TenantTicketCount
);

[PublicAPI]
public sealed record StaffTicketAccount(
    TenantId Id,
    string Name,
    SubscriptionPlan Plan,
    string? LogoUrl,
    PlannedSubscriptionChange? PlannedChange,
    bool HasEverSubscribed
);

[PublicAPI]
public sealed record StaffTicketAssignee(string ObjectId, string DisplayName);

[PublicAPI]
public sealed record StaffTicketMessageView(
    SupportMessageId Id,
    SupportMessageAuthorKind AuthorKind,
    string AuthorDisplayName,
    string Body,
    DateTimeOffset PostedAt,
    TicketAttachmentView[] Attachments
);

[PublicAPI]
public sealed record StaffTicketHistoryEventView(
    SupportTicketHistoryEventType Type,
    SupportMessageAuthorKind ActorKind,
    string ActorDisplayName,
    DateTimeOffset OccurredAt,
    bool HasAttachment,
    string? Payload
);

public sealed class GetTicketDetailForStaffHandler(
    ISupportTicketRepository ticketRepository,
    ITenantRepository tenantRepository,
    ISubscriptionRepository subscriptionRepository,
    IUserRepository userRepository,
    TimeProvider timeProvider
) : IRequestHandler<GetTicketDetailForStaffQuery, Result<StaffTicketDetailResponse>>
{
    public async Task<Result<StaffTicketDetailResponse>> Handle(GetTicketDetailForStaffQuery query, CancellationToken cancellationToken)
    {
        var ticket = await ticketRepository.GetByIdUnfilteredAsync(query.Id, cancellationToken);
        if (ticket is null) return Result<StaffTicketDetailResponse>.NotFound($"Support ticket with id '{query.Id}' not found.");

        var tenant = (await tenantRepository.GetByIdsUnfilteredAsync([ticket.TenantId], cancellationToken)).SingleOrDefault();
        var subscription = (await subscriptionRepository.GetByTenantIdsUnfilteredAsync([ticket.TenantId], cancellationToken)).SingleOrDefault();
        var reporter = (await userRepository.GetByIdsUnfilteredAsync([ticket.ReporterId], cancellationToken)).SingleOrDefault();
        var allTickets = await ticketRepository.GetAllUnfilteredAsync(cancellationToken);
        var reporterTicketCount = allTickets.Count(x => x.ReporterId == ticket.ReporterId);

        var messages = ticket.Messages.Select(m => new StaffTicketMessageView(
                m.Id,
                m.AuthorKind,
                m.AuthorDisplayName,
                m.Body,
                m.PostedAt,
                m.Attachments.Select(a => new TicketAttachmentView(a.FileName, a.ContentType, a.SizeInBytes, SupportTicketAttachmentDownloader.BuildStaffDownloadUrl(ticket.Id, m.Id, a.BlobUrl))).ToArray()
            )
        ).ToArray();

        var history = ticket.HistoryEvents.Select(h => new StaffTicketHistoryEventView(
                h.Type,
                h.ActorKind,
                h.ActorDisplayName,
                h.OccurredAt,
                h.HasAttachment,
                h.Payload
            )
        ).ToArray();

        var csat = ticket.Csat is null ? null : new TicketCsatView(ticket.Csat.Score, ticket.Csat.Comment, ticket.Csat.SubmittedAt);

        return new StaffTicketDetailResponse(
            ticket.Id,
            ticket.ShortDisplayId,
            ticket.Subject,
            ticket.Category,
            ticket.ComputeDisplayStatus(timeProvider.GetUtcNow()),
            new StaffTicketReporter(
                ticket.ReporterId,
                ticket.ReporterEmailSnapshot,
                reporter?.FirstName,
                reporter?.LastName,
                ticket.ReporterRoleSnapshot,
                reporter?.Avatar.Url,
                reporterTicketCount
            ),
            new StaffTicketAccount(
                ticket.TenantId,
                tenant?.Name ?? string.Empty,
                subscription?.Plan ?? tenant?.Plan ?? SubscriptionPlan.Basis,
                tenant?.Logo.Url,
                TenantSummary.ResolvePlannedChange(subscription),
                TenantSummary.ResolveHasEverSubscribed(subscription)
            ),
            ticket.Assignee is null ? null : new StaffTicketAssignee(ticket.Assignee.ObjectId, ticket.Assignee.DisplayName),
            ticket.CreatedAt,
            ticket.LastActivityAt,
            ticket.ResolvedAt,
            ticket.ClosedAt,
            messages,
            history,
            csat
        );
    }
}
