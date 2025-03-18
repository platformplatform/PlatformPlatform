using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Persistence;

namespace PlatformPlatform.AccountManagement.Features.EmailConfirmations.Domain;

public interface IEmailConfirmationRepository : IAppendRepository<EmailConfirmation, EmailConfirmationId>
{
    void Update(EmailConfirmation aggregate);

    EmailConfirmation[] GetByEmail(string email);
}

public sealed class EmailConfirmationRepository(AccountManagementDbContext accountManagementDbContext)
    : RepositoryBase<EmailConfirmation, EmailConfirmationId>(accountManagementDbContext), IEmailConfirmationRepository
{
    public EmailConfirmation[] GetByEmail(string email)
    {
        return DbSet
            .Where(ec => !ec.Completed)
            .Where(ec => ec.Email == email.ToLowerInvariant())
            .ToArray();
    }
}
