using PlatformPlatform.SharedKernel.DomainCore.Entities;
using PlatformPlatform.SharedKernel.DomainCore.Persistence;

namespace PlatformPlatform.AccountManagement.Domain.Users;

public interface IUserRepository : ICrudRepository<User, UserId>
{
    Task<bool> IsEmailFreeAsync(TenantId tenantId, string email, CancellationToken cancellationToken);
    
    Task<int> CountTenantUsersAsync(TenantId tenantId, CancellationToken cancellationToken);
    
    Task<(User[] Users, int TotalItems, int TotalPages)> Search(
        string? search,
        UserRole? userRole,
        SortableUserProperties? orderBy,
        SortOrder? sortOrder,
        int? pageSize,
        int? pageOffset,
        CancellationToken cancellationToken
    );
}
