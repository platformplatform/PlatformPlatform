using PlatformPlatform.AccountManagement.Core.Tenants.Domain;
using PlatformPlatform.SharedKernel.Entities;
using PlatformPlatform.SharedKernel.Persistence;

namespace PlatformPlatform.AccountManagement.Core.Users.Domain;

public interface IUserRepository : ICrudRepository<User, UserId>
{
    Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken);

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
