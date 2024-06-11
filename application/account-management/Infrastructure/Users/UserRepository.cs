using Microsoft.EntityFrameworkCore;
using PlatformPlatform.SharedKernel.DomainCore.Persistence;
using PlatformPlatform.SharedKernel.InfrastructureCore.Persistence;

namespace PlatformPlatform.AccountManagement.Infrastructure.Users;

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
    
    public async Task<(User[] Users, int TotalItems, int TotalPages)> Search(
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
            // Concatenate first and last name to enable searching by full name
            users = users.Where(u => u.Email.Contains(search) || (u.FirstName + " " + u.LastName).Contains(search));
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
        var result = await users.Skip(itemOffset).Take(pageSize.Value).ToArrayAsync(cancellationToken);
        
        var totalItems = pageOffset == 0 && result.Length < pageSize
            ? result.Length // If the first page returns fewer items than page size, skip querying the total count
            : await users.CountAsync(cancellationToken);
        
        var totalPages = (totalItems - 1) / pageSize.Value + 1;
        return (result, totalItems, totalPages);
    }
}
