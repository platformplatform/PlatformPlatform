using PlatformPlatform.AccountManagement.Domain.AccountRegistrations;
using PlatformPlatform.SharedKernel.InfrastructureCore.Persistence;

namespace PlatformPlatform.AccountManagement.Infrastructure.AccountRegistrations;

[UsedImplicitly]
public sealed class AccountRegistrationRepository(AccountManagementDbContext accountManagementDbContext)
    : RepositoryBase<AccountRegistration, AccountRegistrationId>(accountManagementDbContext),
        IAccountRegistrationRepository
{
    public AccountRegistration[] GetByEmail(string email)
    {
        return accountManagementDbContext.AccountRegistrations
            .Where(r => r.Email == email.ToLowerInvariant()).ToArray();
    }
}