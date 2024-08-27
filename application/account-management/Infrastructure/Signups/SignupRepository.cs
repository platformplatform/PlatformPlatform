using PlatformPlatform.AccountManagement.Domain.Signups;
using PlatformPlatform.SharedKernel.InfrastructureCore.Persistence;

namespace PlatformPlatform.AccountManagement.Infrastructure.Signups;

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
