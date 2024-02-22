namespace PlatformPlatform.AccountManagement.Domain.AccountRegistrations;

public interface IAccountRegistrationRepository
{
    Task<AccountRegistration?> GetByIdAsync(AccountRegistrationId id, CancellationToken cancellationToken);

    AccountRegistration[] GetByEmail(string email);

    Task AddAsync(AccountRegistration aggregate, CancellationToken cancellationToken);

    void Update(AccountRegistration aggregate);
}