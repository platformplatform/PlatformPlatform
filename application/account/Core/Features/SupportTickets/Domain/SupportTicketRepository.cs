using Account.Database;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
using SharedKernel.EntityFramework;
using SharedKernel.Persistence;

namespace Account.Features.SupportTickets.Domain;

public interface ISupportTicketRepository : ICrudRepository<SupportTicket, SupportTicketId>
{
    /// <summary>
    ///     Returns the current reporter's tickets, scoped via the (tenant_id, reporter_id) index
    ///     rather than loading every tenant ticket and filtering in memory.
    /// </summary>
    Task<SupportTicket[]> GetByReporterIdAsync(UserId reporterId, CancellationToken cancellationToken);

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
    ///     Returns the current reporter's tickets using the (tenant_id, reporter_id) index. The tenant
    ///     query filter still applies; ordering is left to the caller (SQLite cannot translate
    ///     DateTimeOffset comparisons in ORDER BY).
    /// </summary>
    public async Task<SupportTicket[]> GetByReporterIdAsync(UserId reporterId, CancellationToken cancellationToken)
    {
        return await DbSet.Where(t => t.ReporterId == reporterId).ToArrayAsync(cancellationToken);
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
