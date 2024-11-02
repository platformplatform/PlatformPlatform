using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Persistence;

namespace PlatformPlatform.AccountManagement.Features.Signups.Domain;

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
