using Account.Database;
using Account.Features.Tenants.Domain;
using Account.Features.Users.BackOffice.Queries;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
using SharedKernel.EntityFramework;
using SharedKernel.ExecutionContext;
using SharedKernel.Persistence;

namespace Account.Features.Users.Domain;

public interface IUserRepository : ICrudRepository<User, UserId>, IBulkRemoveRepository<User>, ISoftDeletableRepository<User, UserId>
{
    Task<User?> GetByIdUnfilteredAsync(UserId id, CancellationToken cancellationToken);

    Task<User> GetLoggedInUserAsync(CancellationToken cancellationToken);

    Task<User?> GetUserByEmailUnfilteredAsync(string email, CancellationToken cancellationToken);

    Task<User?> GetDeletedUserByEmailAsync(string email, CancellationToken cancellationToken);

    Task<bool> IsEmailFreeAsync(string email, CancellationToken cancellationToken);

    Task<int> CountTenantUsersAsync(TenantId tenantId, CancellationToken cancellationToken);

    Task<(int TotalUsers, int ActiveUsers, int PendingUsers)> GetUserSummaryAsync(CancellationToken cancellationToken);

    Task<User[]> GetByIdsAsync(UserId[] ids, CancellationToken cancellationToken);

    Task<User[]> GetDeletedByIdsAsync(UserId[] ids, CancellationToken cancellationToken);

    Task<(User[] Users, int TotalItems, int TotalPages)> Search(
        string? search,
        UserRole? userRole,
        UserStatus? userStatus,
        DateTimeOffset? startDate,
        DateTimeOffset? endDate,
        SortableUserProperties? orderBy,
        SortOrder? sortOrder,
        int? pageOffset,
        int? pageSize,
        CancellationToken cancellationToken
    );

    Task<User[]> GetTenantUsers(CancellationToken cancellationToken);

    Task<User[]> GetUsersByEmailUnfilteredAsync(string email, CancellationToken cancellationToken);

    /// <summary>
    ///     Returns total, 30-day active, and pending (unconfirmed email) user counts for the given tenant without applying
    ///     tenant query filters.
    ///     This method is used by back-office cross-tenant queries where tenant context is not established.
    /// </summary>
    Task<(int TotalUsers, int ActiveUsers, int PendingUsers)> GetUserCountsForTenantUnfilteredAsync(TenantId tenantId, DateTimeOffset activeSince, CancellationToken cancellationToken);

    /// <summary>
    ///     Searches users belonging to a specific tenant without applying tenant query filters.
    ///     This method is used by back-office cross-tenant queries where tenant context is not established.
    /// </summary>
    Task<(User[] Users, int TotalItems, int TotalPages)> SearchTenantUsersUnfilteredAsync(
        TenantId tenantId,
        string? search,
        UserRole[] roles,
        int? pageOffset,
        int pageSize,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Searches users across every tenant without applying tenant query filters. Search is required and matches
    ///     user email, full name, or tenant name. The activity filter compares <see cref="User.LastSeenAt" /> to
    ///     a sliding window relative to <paramref name="now" />. This method is used by the back-office cross-tenant
    ///     Users search page where tenant context is not established.
    /// </summary>
    Task<(User[] Users, int TotalItems, int TotalPages)> SearchAllUsersUnfilteredAsync(
        string search,
        UserRole[] roles,
        UserActivityFilter? activity,
        DateTimeOffset now,
        SortableBackOfficeUserProperties orderBy,
        SortOrder sortOrder,
        int pageOffset,
        int pageSize,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Returns every user created at or after <paramref name="since" /> across all tenants without applying tenant
    ///     query filters. Used by the back-office dashboard to compute new-user trend buckets across all tenants.
    /// </summary>
    Task<User[]> GetCreatedSinceUnfilteredAsync(DateTimeOffset since, CancellationToken cancellationToken);

    /// <summary>
    ///     Returns the earliest-created Owner for each of the given tenants without applying tenant query filters.
    ///     Used by the back-office recent signups dashboard to attribute each new tenant to the user who signed up.
    /// </summary>
    Task<Dictionary<TenantId, User>> GetFirstOwnerByTenantIdsUnfilteredAsync(TenantId[] tenantIds, CancellationToken cancellationToken);

    /// <summary>
    ///     Returns every non-deleted user across all tenants without applying tenant query filters.
    ///     Used by the back-office dashboard KPI snapshot to compute period-active users (last_seen_at within
    ///     the selected period) across all tenants. SQLite cannot translate DateTimeOffset comparisons in WHERE,
    ///     so the time filter runs in memory; the user count is bounded by the dashboard's audience.
    /// </summary>
    Task<User[]> GetAllUnfilteredAsync(CancellationToken cancellationToken);
}

public sealed class UserRepository(AccountDbContext accountDbContext, IExecutionContext executionContext, TimeProvider timeProvider)
    : SoftDeletableRepositoryBase<User, UserId>(accountDbContext), IUserRepository
{
    /// <summary>
    ///     Retrieves a user by ID without applying tenant query filters.
    ///     This method should only be used during authentication processes where tenant context is not yet established.
    ///     Soft-deleted users are excluded - they cannot log in.
    /// </summary>
    public async Task<User?> GetByIdUnfilteredAsync(UserId id, CancellationToken cancellationToken)
    {
        return await DbSet
            .IgnoreQueryFilters([QueryFilterNames.Tenant])
            .SingleOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User> GetLoggedInUserAsync(CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(executionContext.UserInfo.Id);
        return await GetByIdAsync(executionContext.UserInfo.Id, cancellationToken) ??
               throw new InvalidOperationException("Logged in user not found.");
    }

    /// <summary>
    ///     Retrieves a user by email without applying tenant query filters.
    ///     This method should only be used during the login processes where tenant context is not yet established.
    ///     Soft-deleted users are excluded - they cannot log in.
    /// </summary>
    public async Task<User?> GetUserByEmailUnfilteredAsync(string email, CancellationToken cancellationToken)
    {
        return await DbSet
            .IgnoreQueryFilters([QueryFilterNames.Tenant])
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);
    }

    public async Task<User?> GetDeletedUserByEmailAsync(string email, CancellationToken cancellationToken)
    {
        return await DbSet
            .IgnoreQueryFilters([QueryFilterNames.SoftDelete])
            .Where(u => u.DeletedAt != null)
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);
    }

    public async Task<bool> IsEmailFreeAsync(string email, CancellationToken cancellationToken)
    {
        return !await DbSet
            .IgnoreQueryFilters([QueryFilterNames.SoftDelete])
            .AnyAsync(u => u.Email == email.ToLowerInvariant(), cancellationToken);
    }

    public Task<int> CountTenantUsersAsync(TenantId tenantId, CancellationToken cancellationToken)
    {
        return DbSet.CountAsync(u => u.TenantId == tenantId, cancellationToken);
    }

    public async Task<User[]> GetByIdsAsync(UserId[] ids, CancellationToken cancellationToken)
    {
        return await DbSet.Where(u => ids.AsEnumerable().Contains(u.Id)).ToArrayAsync(cancellationToken);
    }

    public async Task<User[]> GetDeletedByIdsAsync(UserId[] ids, CancellationToken cancellationToken)
    {
        return await DbSet
            .IgnoreQueryFilters([QueryFilterNames.SoftDelete])
            .Where(u => u.DeletedAt != null)
            .Where(u => ids.AsEnumerable().Contains(u.Id))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<(int TotalUsers, int ActiveUsers, int PendingUsers)> GetUserSummaryAsync(CancellationToken cancellationToken)
    {
        var thirtyDaysAgo = timeProvider.GetUtcNow().AddDays(-30);

        if (accountDbContext.Database.ProviderName is "Microsoft.EntityFrameworkCore.Sqlite")
        {
            var sql = """
                      SELECT
                          COUNT(*) AS total_users,
                          SUM(CASE WHEN email_confirmed = 1 AND last_seen_at >= {0} THEN 1 ELSE 0 END) AS active_users,
                          SUM(CASE WHEN email_confirmed = 0 THEN 1 ELSE 0 END) AS pending_users
                      FROM users
                      WHERE tenant_id = {1} AND deleted_at IS NULL
                      """;

            var result = await accountDbContext.Database
                .SqlQueryRaw<UserSummaryResult>(sql, thirtyDaysAgo.ToString("O"), executionContext.TenantId!.Value.ToString())
                .SingleAsync(cancellationToken);

            return (result.TotalUsers, result.ActiveUsers, result.PendingUsers);
        }

        var totalUsers = await DbSet.CountAsync(cancellationToken);

        var activeUsers = await DbSet
            .Where(u => u.EmailConfirmed)
            .Where(u => u.LastSeenAt >= thirtyDaysAgo)
            .CountAsync(cancellationToken);

        var pendingUsers = await DbSet
            .Where(u => !u.EmailConfirmed)
            .CountAsync(cancellationToken);

        return (totalUsers, activeUsers, pendingUsers);
    }

    public async Task<(User[] Users, int TotalItems, int TotalPages)> Search(
        string? search,
        UserRole? userRole,
        UserStatus? userStatus,
        DateTimeOffset? startDate,
        DateTimeOffset? endDate,
        SortableUserProperties? orderBy,
        SortOrder? sortOrder,
        int? pageOffset,
        int? pageSize,
        CancellationToken cancellationToken
    )
    {
        IQueryable<User> users = DbSet;

        if (search is not null)
        {
            // Concatenate first and last name to enable searching by full name
            users = users.Where(u =>
                u.Email.Contains(search) ||
                (u.FirstName + " " + u.LastName).Contains(search) ||
                (u.Title ?? "").Contains(search)
            );
        }

        if (userRole is not null)
        {
            users = users.Where(u => u.Role == userRole);
        }

        if (userStatus is not null)
        {
            var active = userStatus == UserStatus.Active;
            users = users.Where(u => u.EmailConfirmed == active);
        }

        if (startDate is not null)
        {
            users = users.Where(u => u.ModifiedAt >= startDate);
        }

        if (endDate is not null)
        {
            users = users.Where(u => u.ModifiedAt < endDate.Value.AddDays(1));
        }

        users = orderBy switch
        {
            SortableUserProperties.CreatedAt => sortOrder == SortOrder.Ascending
                ? users.OrderBy(u => u.CreatedAt)
                : users.OrderByDescending(u => u.CreatedAt),
            SortableUserProperties.LastSeenAt => sortOrder == SortOrder.Ascending
                ? users.OrderBy(u => u.LastSeenAt == null ? 1 : 0).ThenBy(u => u.LastSeenAt).ThenByDescending(u => u.CreatedAt)
                : users.OrderBy(u => u.LastSeenAt == null ? 1 : 0).ThenByDescending(u => u.LastSeenAt).ThenByDescending(u => u.CreatedAt),
            SortableUserProperties.Name => sortOrder == SortOrder.Ascending
                ? users.OrderBy(u => u.FirstName == null ? 1 : 0)
                    .ThenBy(u => u.FirstName)
                    .ThenBy(u => u.LastName == null ? 1 : 0)
                    .ThenBy(u => u.LastName)
                    .ThenBy(u => u.Email)
                : users.OrderBy(u => u.FirstName == null ? 0 : 1)
                    .ThenByDescending(u => u.FirstName)
                    .ThenBy(u => u.LastName == null ? 0 : 1)
                    .ThenByDescending(u => u.LastName)
                    .ThenBy(u => u.Email),
            SortableUserProperties.Email => sortOrder == SortOrder.Ascending
                ? users.OrderBy(u => u.Email)
                : users.OrderByDescending(u => u.Email),
            SortableUserProperties.Role => sortOrder == SortOrder.Ascending
                ? users.OrderBy(u => u.Role)
                : users.OrderByDescending(u => u.Role),
            _ => users
                .OrderBy(u => u.FirstName == null ? 1 : 0)
                .ThenBy(u => u.FirstName)
                .ThenBy(u => u.LastName == null ? 1 : 0)
                .ThenBy(u => u.LastName)
                .ThenBy(u => u.Email)
        };

        pageSize ??= 50;
        var itemOffset = (pageOffset ?? 0) * pageSize.Value;
        var result = await users.Skip(itemOffset).Take(pageSize.Value).ToArrayAsync(cancellationToken);

        var totalItems = pageOffset == 0 && result.Length < pageSize
            ? result.Length // If the first page returns fewer items than page size, skip querying the total count
            : await users.CountAsync(cancellationToken);

        var totalPages = totalItems == 0 ? 0 : (totalItems - 1) / pageSize.Value + 1;
        return (result, totalItems, totalPages);
    }

    public async Task<User[]> GetTenantUsers(CancellationToken cancellationToken)
    {
        return await DbSet.ToArrayAsync(cancellationToken);
    }

    /// <summary>
    ///     Retrieves all users with a given email across all tenants without applying tenant query filters.
    ///     This method should only be used during authentication processes where tenant context is not yet established.
    ///     Soft-deleted users are excluded - they cannot log in.
    /// </summary>
    public async Task<User[]> GetUsersByEmailUnfilteredAsync(string email, CancellationToken cancellationToken)
    {
        return await DbSet
            .IgnoreQueryFilters([QueryFilterNames.Tenant])
            .Where(u => u.Email == email.ToLowerInvariant())
            .OrderBy(u => u.Id)
            .ToArrayAsync(cancellationToken);
    }

    /// <summary>
    ///     Returns total, 30-day active, and pending (unconfirmed email) user counts for the given tenant without applying
    ///     tenant query filters.
    ///     This method is used by back-office cross-tenant queries where tenant context is not established.
    /// </summary>
    public async Task<(int TotalUsers, int ActiveUsers, int PendingUsers)> GetUserCountsForTenantUnfilteredAsync(TenantId tenantId, DateTimeOffset activeSince, CancellationToken cancellationToken)
    {
        // SQLite EF cannot translate DateTimeOffset comparisons (text-stored); test path materializes the relevant columns and counts in memory, bounded by tenant size.
        if (accountDbContext.Database.ProviderName is "Microsoft.EntityFrameworkCore.Sqlite")
        {
            var users = await DbSet
                .IgnoreQueryFilters([QueryFilterNames.Tenant])
                .Where(u => u.TenantId == tenantId)
                .Select(u => new { u.LastSeenAt, u.EmailConfirmed })
                .ToListAsync(cancellationToken);
            return (users.Count, users.Count(u => u.EmailConfirmed && u.LastSeenAt.HasValue && u.LastSeenAt.Value >= activeSince), users.Count(u => !u.EmailConfirmed));
        }

        var counts = await DbSet
            .IgnoreQueryFilters([QueryFilterNames.Tenant])
            .Where(u => u.TenantId == tenantId)
            .GroupBy(_ => 1)
            .Select(g => new { Total = g.Count(), Active = g.Count(u => u.EmailConfirmed && u.LastSeenAt >= activeSince), Pending = g.Count(u => !u.EmailConfirmed) })
            .SingleOrDefaultAsync(cancellationToken);

        return (counts?.Total ?? 0, counts?.Active ?? 0, counts?.Pending ?? 0);
    }

    /// <summary>
    ///     Searches users belonging to a specific tenant without applying tenant query filters.
    ///     This method is used by back-office cross-tenant queries where tenant context is not established.
    /// </summary>
    public async Task<(User[] Users, int TotalItems, int TotalPages)> SearchTenantUsersUnfilteredAsync(
        TenantId tenantId,
        string? search,
        UserRole[] roles,
        int? pageOffset,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        var users = DbSet.IgnoreQueryFilters([QueryFilterNames.Tenant]).Where(u => u.TenantId == tenantId);

        if (roles.Length > 0)
        {
            users = users.Where(u => roles.AsEnumerable().Contains(u.Role));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            users = users.Where(u =>
                u.Email.Contains(search) ||
                (u.FirstName + " " + u.LastName).Contains(search) ||
                (u.Title ?? "").Contains(search)
            );
        }

        users = users
            .OrderBy(u => u.FirstName == null ? 1 : 0)
            .ThenBy(u => u.FirstName)
            .ThenBy(u => u.LastName == null ? 1 : 0)
            .ThenBy(u => u.LastName)
            .ThenBy(u => u.Email);

        var itemOffset = (pageOffset ?? 0) * pageSize;
        var result = await users.Skip(itemOffset).Take(pageSize).ToArrayAsync(cancellationToken);

        var totalItems = pageOffset == 0 && result.Length < pageSize
            ? result.Length
            : await users.CountAsync(cancellationToken);

        var totalPages = totalItems == 0 ? 0 : (totalItems - 1) / pageSize + 1;
        return (result, totalItems, totalPages);
    }

    /// <summary>
    ///     Searches users across every tenant without applying tenant query filters. When <paramref name="search" />
    ///     is empty, every user is returned (subject to role/activity filters and pagination). When non-empty,
    ///     matches user email, full name, or tenant name. The activity filter compares <see cref="User.LastSeenAt" />
    ///     to a sliding window relative to <paramref name="now" />. This method is used by the back-office
    ///     cross-tenant Users page where tenant context is not established.
    ///     Search and role filters run in the database. Activity filter, sort, and pagination run in memory because
    ///     SQLite cannot translate DateTimeOffset comparisons in WHERE or ORDER BY clauses (the test database is
    ///     SQLite).
    /// </summary>
    public async Task<(User[] Users, int TotalItems, int TotalPages)> SearchAllUsersUnfilteredAsync(
        string search,
        UserRole[] roles,
        UserActivityFilter? activity,
        DateTimeOffset now,
        SortableBackOfficeUserProperties orderBy,
        SortOrder sortOrder,
        int pageOffset,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        var users = DbSet.IgnoreQueryFilters([QueryFilterNames.Tenant]);

        if (!string.IsNullOrEmpty(search))
        {
            // Tenant name search is implemented as a separate lookup so we don't need an EF join. We then OR the
            // resulting ids into the user predicate alongside email and full-name matches.
            var matchingTenantIds = await accountDbContext.Set<Tenant>()
                .IgnoreQueryFilters([QueryFilterNames.Tenant])
                .Where(t => t.Name.ToLower().Contains(search))
                .Select(t => t.Id)
                .ToArrayAsync(cancellationToken);

            users = users.Where(u =>
                u.Email.Contains(search) ||
                ((u.FirstName ?? "") + " " + (u.LastName ?? "")).ToLower().Contains(search) ||
                matchingTenantIds.AsEnumerable().Contains(u.TenantId)
            );
        }

        if (roles.Length > 0)
        {
            users = users.Where(u => roles.AsEnumerable().Contains(u.Role));
        }

        var candidates = await users.ToArrayAsync(cancellationToken);

        if (activity is not null)
        {
            var oneDayAgo = now.AddDays(-1);
            var sevenDaysAgo = now.AddDays(-7);
            var thirtyDaysAgo = now.AddDays(-30);
            candidates = activity switch
            {
                UserActivityFilter.ActiveLast24Hours => candidates.Where(u => u.LastSeenAt >= oneDayAgo).ToArray(),
                UserActivityFilter.ActiveLast7Days => candidates.Where(u => u.LastSeenAt >= sevenDaysAgo).ToArray(),
                UserActivityFilter.ActiveLast30Days => candidates.Where(u => u.LastSeenAt >= thirtyDaysAgo).ToArray(),
                UserActivityFilter.InactiveOver30Days => candidates.Where(u => u.LastSeenAt is null || u.LastSeenAt < thirtyDaysAgo).ToArray(),
                _ => candidates
            };
        }

        IEnumerable<User> ordered = (orderBy, sortOrder) switch
        {
            (SortableBackOfficeUserProperties.Email, SortOrder.Ascending) => candidates.OrderBy(u => u.Email),
            (SortableBackOfficeUserProperties.Email, _) => candidates.OrderByDescending(u => u.Email),
            (SortableBackOfficeUserProperties.Role, SortOrder.Ascending) => candidates.OrderBy(u => u.Role).ThenBy(u => u.Email),
            (SortableBackOfficeUserProperties.Role, _) => candidates.OrderByDescending(u => u.Role).ThenBy(u => u.Email),
            (SortableBackOfficeUserProperties.LastSeenAt, SortOrder.Ascending) => candidates.OrderBy(u => u.LastSeenAt ?? DateTimeOffset.MinValue).ThenBy(u => u.Email),
            (SortableBackOfficeUserProperties.LastSeenAt, _) => candidates.OrderByDescending(u => u.LastSeenAt ?? DateTimeOffset.MinValue).ThenBy(u => u.Email),
            (SortableBackOfficeUserProperties.CreatedAt, SortOrder.Ascending) => candidates.OrderBy(u => u.CreatedAt),
            (SortableBackOfficeUserProperties.CreatedAt, _) => candidates.OrderByDescending(u => u.CreatedAt),
            (_, SortOrder.Descending) => candidates
                .OrderBy(u => u.FirstName is null ? 0 : 1)
                .ThenByDescending(u => u.FirstName)
                .ThenBy(u => u.LastName is null ? 0 : 1)
                .ThenByDescending(u => u.LastName)
                .ThenBy(u => u.Email),
            _ => candidates
                .OrderBy(u => u.FirstName is null ? 1 : 0)
                .ThenBy(u => u.FirstName)
                .ThenBy(u => u.LastName is null ? 1 : 0)
                .ThenBy(u => u.LastName)
                .ThenBy(u => u.Email)
        };

        var totalItems = candidates.Length;
        var totalPages = totalItems == 0 ? 0 : (totalItems - 1) / pageSize + 1;
        var pageUsers = ordered.Skip(pageOffset * pageSize).Take(pageSize).ToArray();
        return (pageUsers, totalItems, totalPages);
    }

    /// <summary>
    ///     Returns every user created at or after <paramref name="since" /> across all tenants without applying tenant
    ///     query filters. Used by the back-office dashboard to compute new-user trend buckets across all tenants.
    ///     SQLite cannot translate DateTimeOffset comparisons in WHERE, so the time filter runs in memory; the
    ///     dashboard period is bounded (max 90 days) so the materialized set stays small.
    /// </summary>
    public async Task<User[]> GetCreatedSinceUnfilteredAsync(DateTimeOffset since, CancellationToken cancellationToken)
    {
        var users = await DbSet.IgnoreQueryFilters([QueryFilterNames.Tenant]).ToArrayAsync(cancellationToken);
        return users.Where(u => u.CreatedAt >= since).ToArray();
    }

    /// <summary>
    ///     Returns every non-deleted user across all tenants without applying tenant query filters.
    ///     Used by the back-office dashboard KPI snapshot to compute period-active users (last_seen_at within
    ///     the selected period) across all tenants. SQLite cannot translate DateTimeOffset comparisons in WHERE,
    ///     so the time filter runs in memory; the user count is bounded by the dashboard's audience.
    /// </summary>
    public async Task<User[]> GetAllUnfilteredAsync(CancellationToken cancellationToken)
    {
        return await DbSet.IgnoreQueryFilters([QueryFilterNames.Tenant]).ToArrayAsync(cancellationToken);
    }

    /// <summary>
    ///     Returns the earliest-created Owner for each of the given tenants without applying tenant query filters.
    ///     Used by the back-office recent signups dashboard to attribute each new tenant to the user who signed up.
    /// </summary>
    public async Task<Dictionary<TenantId, User>> GetFirstOwnerByTenantIdsUnfilteredAsync(TenantId[] tenantIds, CancellationToken cancellationToken)
    {
        if (tenantIds.Length == 0) return new Dictionary<TenantId, User>();

        // SQLite cannot translate DateTimeOffset ORDER BY clauses, so materialize the candidate Owners and pick
        // the earliest in memory. Bounded by the number of tenants on the dashboard recent-signups list.
        var owners = await DbSet
            .IgnoreQueryFilters([QueryFilterNames.Tenant])
            .Where(u => u.Role == UserRole.Owner && tenantIds.AsEnumerable().Contains(u.TenantId))
            .ToArrayAsync(cancellationToken);

        return owners
            .GroupBy(u => u.TenantId)
            .ToDictionary(g => g.Key, g => g.OrderBy(u => u.CreatedAt).ThenBy(u => u.Id.Value).First());
    }

    [UsedImplicitly]
    private sealed record UserSummaryResult(int TotalUsers, int ActiveUsers, int PendingUsers);
}
