using Account.Database;
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

    Task<User[]> GetByIdsAsync(UserId[] ids, CancellationToken cancellationToken);

    Task<User[]> GetDeletedByIdsAsync(UserId[] ids, CancellationToken cancellationToken);

    Task<User[]> GetUsersByEmailUnfilteredAsync(string email, CancellationToken cancellationToken);
}

internal sealed class UserRepository(AccountDbContext accountDbContext, IExecutionContext executionContext)
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
}
