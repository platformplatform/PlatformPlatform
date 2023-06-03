using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.AccountManagement.Domain.Users;
using PlatformPlatform.SharedKernel.InfrastructureCore.Persistence;

namespace PlatformPlatform.AccountManagement.Infrastructure.Users;

[UsedImplicitly]
internal sealed class UserRepository : RepositoryBase<User, UserId>, IUserRepository
{
    public UserRepository(AccountManagementDbContext accountManagementDbContext) : base(accountManagementDbContext)
    {
    }

    public async Task<bool> IsEmailFreeAsync(TenantId tenantId, string email, CancellationToken cancellationToken)
    {
        return await DbSet.AnyAsync(u => u.TenantId == tenantId && u.Email == email, cancellationToken);
    }
}