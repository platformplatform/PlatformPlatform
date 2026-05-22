using Account.Database;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
using SharedKernel.EntityFramework;
using SharedKernel.Persistence;

namespace Account.Features.SupportTickets.Domain;

public interface ISupportTicketRepository : ICrudRepository<SupportTicket, SupportTicketId>
{
    Task<SupportTicket[]> GetTenantTicketsAsync(CancellationToken cancellationToken);

    Task<int> CountTenantTicketsAwaitingUserAsync(UserId reporterId, CancellationToken cancellationToken);

    /// <summary>
    ///     Retrieves a ticket without applying tenant query filters. Used by the back-office
    ///     cross-tenant detail page where tenant context is not established.
    /// </summary>
    Task<SupportTicket?> GetByIdUnfilteredAsync(SupportTicketId id, CancellationToken cancellationToken);

    /// <summary>
    ///     Returns every ticket across all tenants without applying tenant query filters. Used by
    ///     the back-office cross-tenant inbox. Filtering, sorting, and pagination are performed in
    ///     memory by the caller (SQLite cannot translate DateTimeOffset comparisons in WHERE/ORDER BY
    ///     for the per-row LastActivityAt column the inbox sorts by).
    /// </summary>
    Task<SupportTicket[]> GetAllUnfilteredAsync(CancellationToken cancellationToken);
}

public sealed class SupportTicketRepository(AccountDbContext accountDbContext)
    : RepositoryBase<SupportTicket, SupportTicketId>(accountDbContext), ISupportTicketRepository
{
    /// <summary>
    ///     Overrides the base GetByIdAsync to respect the tenant query filter. The base uses
    ///     DbSet.FindAsync which bypasses every EF query filter, including the tenant filter; for a
    ///     tenant-scoped aggregate that would return rows from any tenant and leave isolation to the
    ///     per-handler reporter check alone. Checks the local change tracker first so an aggregate
    ///     already loaded in the request scope is returned without a round trip.
    /// </summary>
    public new async Task<SupportTicket?> GetByIdAsync(SupportTicketId id, CancellationToken cancellationToken)
    {
        var local = DbSet.Local.SingleOrDefault(e => e.Id.Equals(id));
        if (local is not null) return local;

        return await DbSet.SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    /// <summary>
    ///     Returns every ticket for the current tenant. Sort happens in memory because SQLite (test
    ///     database) cannot translate DateTimeOffset comparisons in ORDER BY; the tenant-scoped result
    ///     set is bounded by how many tickets a tenant can reasonably open in v1.
    /// </summary>
    public async Task<SupportTicket[]> GetTenantTicketsAsync(CancellationToken cancellationToken)
    {
        var tickets = await DbSet.ToArrayAsync(cancellationToken);
        return tickets.OrderByDescending(t => t.LastActivityAt).ToArray();
    }

    public async Task<int> CountTenantTicketsAwaitingUserAsync(UserId reporterId, CancellationToken cancellationToken)
    {
        return await DbSet
            .Where(t => t.ReporterId == reporterId && t.Status == SupportTicketStatus.AwaitingUser)
            .CountAsync(cancellationToken);
    }

    /// <summary>
    ///     Retrieves a ticket without applying tenant query filters. Used by the back-office
    ///     cross-tenant detail page where tenant context is not established.
    /// </summary>
    public async Task<SupportTicket?> GetByIdUnfilteredAsync(SupportTicketId id, CancellationToken cancellationToken)
    {
        return await DbSet.IgnoreQueryFilters([QueryFilterNames.Tenant]).SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    /// <summary>
    ///     Returns every ticket across all tenants without applying tenant query filters. Used by
    ///     the back-office cross-tenant inbox.
    /// </summary>
    public async Task<SupportTicket[]> GetAllUnfilteredAsync(CancellationToken cancellationToken)
    {
        return await DbSet.IgnoreQueryFilters([QueryFilterNames.Tenant]).ToArrayAsync(cancellationToken);
    }
}
