using Account.Features.SupportTickets.Domain;
using JetBrains.Annotations;
using SharedKernel.Cqrs;
using SharedKernel.ExecutionContext;

namespace Account.Features.SupportTickets.Queries;

[PublicAPI]
public sealed record GetMyTicketsQuery : IRequest<Result<MyTicketsResponse>>;

[PublicAPI]
public sealed record MyTicketsResponse(MyTicketSummary[] Active, MyTicketSummary[] Closed, int AwaitingUserCount);

[PublicAPI]
public sealed record MyTicketSummary(
    SupportTicketId Id,
    string ShortDisplayId,
    string Subject,
    SupportTicketCategory Category,
    SupportTicketStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastActivityAt,
    int MessagesCount,
    int AttachmentsCount
);

public sealed class GetMyTicketsHandler(ISupportTicketRepository ticketRepository, IExecutionContext executionContext, TimeProvider timeProvider)
    : IRequestHandler<GetMyTicketsQuery, Result<MyTicketsResponse>>
{
    public async Task<Result<MyTicketsResponse>> Handle(GetMyTicketsQuery query, CancellationToken cancellationToken)
    {
        var reporterId = executionContext.UserInfo.Id!;
        var owned = await ticketRepository.GetByReporterIdAsync(reporterId, cancellationToken);
        var now = timeProvider.GetUtcNow();

        var active = owned
            .Where(t => t.ComputeDisplayStatus(now) is not SupportTicketStatus.Closed)
            .OrderByDescending(t => t.LastActivityAt)
            .Select(t => ToSummary(t, now))
            .ToArray();
        var closed = owned
            .Where(t => t.ComputeDisplayStatus(now) is SupportTicketStatus.Closed)
            .OrderByDescending(t => t.LastActivityAt)
            .Select(t => ToSummary(t, now))
            .ToArray();
        var awaitingUser = owned.Count(t => t.ComputeDisplayStatus(now) is SupportTicketStatus.AwaitingUser);

        return new MyTicketsResponse(active, closed, awaitingUser);
    }

    private static MyTicketSummary ToSummary(SupportTicket ticket, DateTimeOffset now)
    {
        var publicMessages = ticket.Messages.Where(m => m.AuthorKind != SupportMessageAuthorKind.Internal).ToArray();
        return new MyTicketSummary(
            ticket.Id,
            ticket.ShortDisplayId,
            ticket.Subject,
            ticket.Category,
            ticket.ComputeDisplayStatus(now),
            ticket.CreatedAt,
            ticket.LastActivityAt,
            publicMessages.Length,
            publicMessages.Sum(m => m.Attachments.Length)
        );
    }
}
