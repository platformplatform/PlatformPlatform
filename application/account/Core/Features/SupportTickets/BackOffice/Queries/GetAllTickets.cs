using System.Security.Claims;
using Account.Features.Subscriptions.Domain;
using Account.Features.SupportTickets.Domain;
using Account.Features.Tenants.Domain;
using Account.Features.Users.Domain;
using FluentValidation;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using SharedKernel.Cqrs;
using SharedKernel.Domain;
using SharedKernel.Persistence;

namespace Account.Features.SupportTickets.BackOffice.Queries;

[PublicAPI]
public sealed record GetAllTicketsQuery(
    string? Search = null,
    SupportTicketStatus? Status = null,
    SupportTicketCategory? Category = null,
    SupportTicketAssigneeFilter Assignee = SupportTicketAssigneeFilter.Any,
    string? AssigneeObjectId = null,
    UserId? ReporterId = null,
    TenantId? TenantId = null,
    SortableTicketProperties OrderBy = SortableTicketProperties.LastActivity,
    SortOrder SortOrder = SortOrder.Descending,
    int PageOffset = 0,
    int PageSize = 25
) : IRequest<Result<AllTicketsResponse>>
{
    public string? Search { get; } = Search?.Trim();
}

[PublicAPI]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SortableTicketProperties
{
    Subject,
    Status,
    Category,
    Tenant,
    Reporter,
    LastActivity,
    Created,
    Csat,
    Assignee
}

[PublicAPI]
public sealed record AllTicketsResponse(
    int TotalCount,
    int PageSize,
    int TotalPages,
    int CurrentPageOffset,
    AllTicketsCounts Counts,
    AllTicketsSummary[] Tickets
);

[PublicAPI]
public sealed record AllTicketsCounts(int New, int AwaitingAgent, int AwaitingUser, int AwaitingInternal, int ResolvedLast24Hours);

[PublicAPI]
public sealed record AllTicketsSummary(
    SupportTicketId Id,
    string ShortDisplayId,
    string Subject,
    SupportTicketCategory Category,
    SupportTicketStatus Status,
    TenantId TenantId,
    string TenantName,
    string? TenantLogoUrl,
    SubscriptionPlan TenantPlan,
    UserId ReporterId,
    string ReporterEmail,
    string? ReporterName,
    string? ReporterAvatarUrl,
    string ReporterRoleSnapshot,
    AllTicketsAssignee? Assignee,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastActivityAt,
    bool IsUnreadForStaff,
    SupportTicketCsatScore? CsatScore,
    int MessageCount,
    int AttachmentCount
);

[PublicAPI]
public sealed record AllTicketsAssignee(string ObjectId, string DisplayName);

public sealed class GetAllTicketsQueryValidator : AbstractValidator<GetAllTicketsQuery>
{
    public GetAllTicketsQueryValidator()
    {
        RuleFor(x => x.Search!).MaximumLength(200).WithMessage("Search must be at most 200 characters.")
            .When(x => x.Search is not null);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");
        RuleFor(x => x.PageOffset).GreaterThanOrEqualTo(0).WithMessage("Page offset must be greater than or equal to 0.");
    }
}

public sealed class GetAllTicketsHandler(
    ISupportTicketRepository ticketRepository,
    ITenantRepository tenantRepository,
    ISubscriptionRepository subscriptionRepository,
    IUserRepository userRepository,
    IHttpContextAccessor httpContextAccessor,
    TimeProvider timeProvider
) : IRequestHandler<GetAllTicketsQuery, Result<AllTicketsResponse>>
{
    public async Task<Result<AllTicketsResponse>> Handle(GetAllTicketsQuery query, CancellationToken cancellationToken)
    {
        var all = await ticketRepository.GetAllUnfilteredAsync(cancellationToken);

        var now = timeProvider.GetUtcNow();
        // The Resolved tile counts resolution events in the last 24 hours regardless of whether the
        // reporter has since rated and tipped the row into computed Closed. Rate-then-display-Closed
        // tickets are still recent resolutions worth surfacing on the inbox.
        var resolvedLast24Hours = all.Count(t => t.ResolvedAt is not null && t.ResolvedAt.Value >= now.AddHours(-24));
        var counts = new AllTicketsCounts(
            all.Count(t => t.ComputeDisplayStatus(now) is SupportTicketStatus.New),
            all.Count(t => t.ComputeDisplayStatus(now) is SupportTicketStatus.AwaitingAgent),
            all.Count(t => t.ComputeDisplayStatus(now) is SupportTicketStatus.AwaitingUser),
            all.Count(t => t.ComputeDisplayStatus(now) is SupportTicketStatus.AwaitingInternal),
            resolvedLast24Hours
        );

        var meObjectId = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var filtered = all.AsEnumerable();

        if (query.Status is { } status) filtered = filtered.Where(t => t.ComputeDisplayStatus(now) == status);
        if (query.Category is { } category) filtered = filtered.Where(t => t.Category == category);
        if (query.ReporterId is { } reporterId) filtered = filtered.Where(t => t.ReporterId == reporterId);
        if (query.TenantId is { } tenantId) filtered = filtered.Where(t => t.TenantId == tenantId);
        filtered = query.Assignee switch
        {
            SupportTicketAssigneeFilter.Unassigned => filtered.Where(t => t.Assignee is null),
            SupportTicketAssigneeFilter.Me => filtered.Where(t => t.Assignee is not null && t.Assignee.ObjectId == meObjectId),
            _ when !string.IsNullOrEmpty(query.AssigneeObjectId) => filtered.Where(t => t.Assignee is not null && t.Assignee.ObjectId == query.AssigneeObjectId),
            _ => filtered
        };

        // Tenants, subscriptions, and reporters are loaded up front so the search predicate can match
        // tenant names and reporter full names. The same lookups are reused below for hydration.
        var tenantIds = all.Select(t => t.TenantId).Distinct().ToArray();
        var tenants = (await tenantRepository.GetByIdsUnfilteredAsync(tenantIds, cancellationToken)).ToDictionary(t => t.Id);
        var subscriptions = (await subscriptionRepository.GetByTenantIdsUnfilteredAsync(tenantIds, cancellationToken)).ToDictionary(s => s.TenantId);
        var reporterIds = all.Select(t => t.ReporterId).Distinct().ToArray();
        var reporters = (await userRepository.GetByIdsUnfilteredAsync(reporterIds, cancellationToken)).ToDictionary(u => u.Id);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search;
            filtered = filtered.Where(t => TicketMatchesSearch(t, search, tenants, reporters));
        }

        var matches = SortTickets(filtered, query.OrderBy, query.SortOrder, tenants, reporters, now).ToArray();
        var totalCount = matches.Length;
        var totalPages = totalCount == 0 ? 0 : (totalCount - 1) / query.PageSize + 1;
        if (query.PageOffset > 0 && query.PageOffset >= totalPages)
        {
            return Result<AllTicketsResponse>.BadRequest($"The page offset '{query.PageOffset}' is greater than the total number of pages.");
        }

        var page = matches.Skip(query.PageOffset * query.PageSize).Take(query.PageSize).ToArray();

        var summaries = page.Select(t =>
            {
                var tenant = tenants.GetValueOrDefault(t.TenantId);
                var subscription = subscriptions.GetValueOrDefault(t.TenantId);
                var reporter = reporters.GetValueOrDefault(t.ReporterId);
                var assignee = t.Assignee is null ? null : new AllTicketsAssignee(t.Assignee.ObjectId, t.Assignee.DisplayName);
                // Only count public messages and their attachments. Internal staff notes are not part
                // of what the customer or surface counts as conversation activity.
                var publicMessages = t.Messages.Where(message => message.AuthorKind != SupportMessageAuthorKind.Internal).ToArray();
                var attachmentCount = publicMessages.Sum(message => message.Attachments.Length);
                return new AllTicketsSummary(
                    t.Id,
                    t.ShortDisplayId,
                    t.Subject,
                    t.Category,
                    t.ComputeDisplayStatus(now),
                    t.TenantId,
                    tenant?.Name ?? string.Empty,
                    tenant?.Logo.Url,
                    subscription?.Plan ?? tenant?.Plan ?? SubscriptionPlan.Basis,
                    t.ReporterId,
                    t.ReporterEmailSnapshot,
                    reporter is null ? null : $"{reporter.FirstName} {reporter.LastName}".Trim(),
                    reporter?.Avatar.Url,
                    t.ReporterRoleSnapshot,
                    assignee,
                    t.CreatedAt,
                    t.LastActivityAt,
                    IsUnreadForStaff(t),
                    t.Csat?.Score,
                    publicMessages.Length,
                    attachmentCount
                );
            }
        ).ToArray();

        return new AllTicketsResponse(totalCount, query.PageSize, totalPages, query.PageOffset, counts, summaries);
    }

    // Sorts by the column selected in the inbox header. Tenant and Reporter use the already-loaded
    // dictionaries so display order matches what the row renders. LastActivity is the default and
    // also serves as the secondary key for non-time-based columns so equal values stay deterministic.
    private static IEnumerable<SupportTicket> SortTickets(
        IEnumerable<SupportTicket> tickets,
        SortableTicketProperties orderBy,
        SortOrder sortOrder,
        Dictionary<TenantId, Tenant> tenants,
        Dictionary<UserId, User> reporters,
        DateTimeOffset now
    )
    {
        var descending = sortOrder == SortOrder.Descending;
        return orderBy switch
        {
            SortableTicketProperties.Subject => descending
                ? tickets.OrderByDescending(t => t.Subject).ThenByDescending(t => t.LastActivityAt)
                : tickets.OrderBy(t => t.Subject).ThenByDescending(t => t.LastActivityAt),
            // Sort by the computed display status so the visible column order matches the chip the
            // user sees on each row. Otherwise a rated Resolved (computed Closed) would sort with
            // raw Resolved instead of with Closed.
            SortableTicketProperties.Status => descending
                ? tickets.OrderByDescending(t => t.ComputeDisplayStatus(now)).ThenByDescending(t => t.LastActivityAt)
                : tickets.OrderBy(t => t.ComputeDisplayStatus(now)).ThenByDescending(t => t.LastActivityAt),
            SortableTicketProperties.Category => descending
                ? tickets.OrderByDescending(t => t.Category).ThenByDescending(t => t.LastActivityAt)
                : tickets.OrderBy(t => t.Category).ThenByDescending(t => t.LastActivityAt),
            SortableTicketProperties.Tenant => descending
                ? tickets.OrderByDescending(t => TenantName(t, tenants)).ThenByDescending(t => t.LastActivityAt)
                : tickets.OrderBy(t => TenantName(t, tenants)).ThenByDescending(t => t.LastActivityAt),
            SortableTicketProperties.Reporter => descending
                ? tickets.OrderByDescending(t => ReporterDisplay(t, reporters)).ThenByDescending(t => t.LastActivityAt)
                : tickets.OrderBy(t => ReporterDisplay(t, reporters)).ThenByDescending(t => t.LastActivityAt),
            SortableTicketProperties.Created => descending
                ? tickets.OrderByDescending(t => t.CreatedAt)
                : tickets.OrderBy(t => t.CreatedAt),
            // Tickets without a CSAT score are sorted to the end regardless of direction so the column
            // shows the rated tickets first; otherwise nulls would dominate the visible page.
            SortableTicketProperties.Csat => descending
                ? tickets.OrderBy(t => t.Csat is null).ThenByDescending(t => t.Csat?.Score).ThenByDescending(t => t.LastActivityAt)
                : tickets.OrderBy(t => t.Csat is null).ThenBy(t => t.Csat?.Score).ThenByDescending(t => t.LastActivityAt),
            // Unassigned tickets are sorted to the end regardless of direction so assignees are visible
            // first; otherwise unassigned rows would dominate the visible page.
            SortableTicketProperties.Assignee => descending
                ? tickets.OrderBy(t => t.Assignee is null).ThenByDescending(t => t.Assignee?.DisplayName).ThenByDescending(t => t.LastActivityAt)
                : tickets.OrderBy(t => t.Assignee is null).ThenBy(t => t.Assignee?.DisplayName).ThenByDescending(t => t.LastActivityAt),
            _ => descending
                ? tickets.OrderByDescending(t => t.LastActivityAt)
                : tickets.OrderBy(t => t.LastActivityAt)
        };
    }

    private static string TenantName(SupportTicket ticket, Dictionary<TenantId, Tenant> tenants)
    {
        return tenants.GetValueOrDefault(ticket.TenantId)?.Name ?? string.Empty;
    }

    private static string ReporterDisplay(SupportTicket ticket, Dictionary<UserId, User> reporters)
    {
        var reporter = reporters.GetValueOrDefault(ticket.ReporterId);
        if (reporter is null) return ticket.ReporterEmailSnapshot;
        var fullName = $"{reporter.FirstName} {reporter.LastName}".Trim();
        return fullName.Length > 0 ? fullName : ticket.ReporterEmailSnapshot;
    }

    // A ticket is "unread" for staff when the most recent message was authored by the user and the
    // ticket has not been picked up yet (AwaitingAgent or New). Approximation that matches what the
    // design surfaces visually as bold rows. There is no per-staff seen state in v1.
    private static bool IsUnreadForStaff(SupportTicket ticket)
    {
        if (ticket.Status is not (SupportTicketStatus.New or SupportTicketStatus.AwaitingAgent)) return false;
        var lastPublic = ticket.Messages.LastOrDefault(m => m.AuthorKind != SupportMessageAuthorKind.Internal);
        return lastPublic is { AuthorKind: SupportMessageAuthorKind.User };
    }

    // Search matches subject, short display id, reporter email/full name, tenant name, and any
    // message body. Internal staff notes are included because back-office staff can see them, so
    // they should also be searchable from the inbox.
    private static bool TicketMatchesSearch(SupportTicket ticket, string search, Dictionary<TenantId, Tenant> tenants, Dictionary<UserId, User> reporters)
    {
        if (ticket.Subject.Contains(search, StringComparison.OrdinalIgnoreCase)) return true;
        if (ticket.ShortDisplayId.Contains(search, StringComparison.OrdinalIgnoreCase)) return true;
        if (ticket.ReporterEmailSnapshot.Contains(search, StringComparison.OrdinalIgnoreCase)) return true;
        if (ticket.Messages.Any(m => m.Body.Contains(search, StringComparison.OrdinalIgnoreCase))) return true;
        if (tenants.TryGetValue(ticket.TenantId, out var tenant) && tenant.Name.Contains(search, StringComparison.OrdinalIgnoreCase)) return true;
        if (reporters.TryGetValue(ticket.ReporterId, out var reporter))
        {
            var fullName = $"{reporter.FirstName} {reporter.LastName}".Trim();
            if (fullName.Length > 0 && fullName.Contains(search, StringComparison.OrdinalIgnoreCase)) return true;
        }

        return false;
    }
}
