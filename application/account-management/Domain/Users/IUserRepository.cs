using PlatformPlatform.SharedKernel.DomainCore.Persistence;

namespace PlatformPlatform.AccountManagement.Domain.Users;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken);

    Task AddAsync(User aggregate, CancellationToken cancellationToken);

    void Update(User aggregate);

    void Remove(User aggregate);

    Task<bool> IsEmailFreeAsync(TenantId tenantId, string email, CancellationToken cancellationToken);

    Task<int> CountTenantUsersAsync(TenantId tenantId, CancellationToken cancellationToken);

    Task<User[]> Search(
        string? search,
        UserRole? userRole,
        SortableUserProperties? orderBy,
        SortOrder? sortOrder,
        int? pageSize,
        int? pageOffset,
        CancellationToken cancellationToken
    );
}