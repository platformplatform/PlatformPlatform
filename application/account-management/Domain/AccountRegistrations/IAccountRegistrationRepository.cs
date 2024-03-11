namespace PlatformPlatform.AccountManagement.Domain.AccountRegistrations;

public interface IAccountRegistrationRepository
{
    Task<AccountRegistration?> GetByIdAsync(AccountRegistrationId id, CancellationToken cancellationToken);

    AccountRegistration[] GetByEmailOrTenantId(TenantId tenantId, string email);

    Task AddAsync(AccountRegistration aggregate, CancellationToken cancellationToken);

    void Update(AccountRegistration aggregate);
}