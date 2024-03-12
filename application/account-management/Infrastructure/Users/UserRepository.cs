using Microsoft.EntityFrameworkCore;
using PlatformPlatform.SharedKernel.DomainCore.Persistence;
using PlatformPlatform.SharedKernel.InfrastructureCore.Persistence;

namespace PlatformPlatform.AccountManagement.Infrastructure.Users;

[UsedImplicitly]
internal sealed class UserRepository(AccountManagementDbContext accountManagementDbContext)
    : RepositoryBase<User, UserId>(accountManagementDbContext), IUserRepository
{
    public async Task<bool> IsEmailFreeAsync(TenantId tenantId, string email, CancellationToken cancellationToken)
    {
        return !await DbSet.AnyAsync(u => u.TenantId == tenantId && u.Email == email, cancellationToken);
    }

    public Task<int> CountTenantUsersAsync(TenantId tenantId, CancellationToken cancellationToken)
    {
        return DbSet.CountAsync(u => u.TenantId == tenantId, cancellationToken);
    }

    public async Task<User[]> Search(
        string? search,
        UserRole? userRole,
        SortableUserProperties? orderBy,
        SortOrder? sortOrder,
        int? pageSize,
        int? pageOffset,
        CancellationToken cancellationToken
    )
    {
        IQueryable<User> users = DbSet;

        if (search is not null)
        {
            // We use the null-forgiving (!) operator here because the SQL LIKE operator handles NULL values gracefully
            users = users.Where(u =>
                u.Email.Contains(search) || u.FirstName!.Contains(search) || u.LastName!.Contains(search));
        }

        if (userRole is not null)
        {
            users = users.Where(u => u.UserRole == userRole);
        }

        users = orderBy switch
        {
            SortableUserProperties.CreatedAt => sortOrder == SortOrder.Ascending
                ? users.OrderBy(u => u.CreatedAt)
                : users.OrderByDescending(u => u.CreatedAt),
            SortableUserProperties.ModifiedAt => sortOrder == SortOrder.Ascending
                ? users.OrderBy(u => u.ModifiedAt)
                : users.OrderByDescending(u => u.ModifiedAt),
            SortableUserProperties.Name => sortOrder == SortOrder.Ascending
                ? users.OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
                : users.OrderByDescending(u => u.FirstName).ThenByDescending(u => u.LastName),
            SortableUserProperties.Email => sortOrder == SortOrder.Ascending
                ? users.OrderBy(u => u.Email)
                : users.OrderByDescending(u => u.Email),
            SortableUserProperties.UserRole => sortOrder == SortOrder.Ascending
                ? users.OrderBy(u => u.UserRole)
                : users.OrderByDescending(u => u.UserRole),
            _ => users
        };

        pageSize ??= 50;
        var itemOffset = (pageOffset ?? 0) * pageSize.Value;
        return await users.Skip(itemOffset).Take(pageSize.Value).ToArrayAsync(cancellationToken);
    }
}