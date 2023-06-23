using PlatformPlatform.SharedKernel.DomainCore.Persistence;

namespace PlatformPlatform.AccountManagement.Domain.Users;

public interface IUserRepository : IRepository<User, UserId>
{
    Task<bool> IsEmailFreeAsync(TenantId tenantId, string email, CancellationToken cancellationToken);

    Task<int> CountTenantUsersAsync(TenantId tenantId, CancellationToken cancellationToken);
}