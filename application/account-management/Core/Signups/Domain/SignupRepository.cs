using PlatformPlatform.AccountManagement.Core.Database;
using PlatformPlatform.AccountManagement.Core.Tenants.Domain;
using PlatformPlatform.SharedKernel.Entities;
using PlatformPlatform.SharedKernel.Persistence;

namespace PlatformPlatform.AccountManagement.Core.Signups.Domain;

public interface ISignupRepository : ICrudRepository<Signup, SignupId>
{
    Signup[] GetByEmailOrTenantId(TenantId tenantId, string email);
}

public sealed class SignupRepository(AccountManagementDbContext accountManagementDbContext)
    : RepositoryBase<Signup, SignupId>(accountManagementDbContext), ISignupRepository
{
    public Signup[] GetByEmailOrTenantId(TenantId tenantId, string email)
    {
        return accountManagementDbContext.Signups
            .Where(r => !r.Completed)
            .Where(r => r.TenantId == tenantId || r.Email == email.ToLowerInvariant())
            .ToArray();
    }
}
