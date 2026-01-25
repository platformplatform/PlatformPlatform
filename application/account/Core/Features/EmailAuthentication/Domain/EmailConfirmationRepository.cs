using PlatformPlatform.Account.Database;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Persistence;

namespace PlatformPlatform.Account.Features.EmailAuthentication.Domain;

public interface IEmailConfirmationRepository : IAppendRepository<EmailConfirmation, EmailConfirmationId>
{
    void Update(EmailConfirmation aggregate);

    EmailConfirmation[] GetByEmail(string email);
}

public sealed class EmailConfirmationRepository(AccountDbContext accountDbContext)
    : RepositoryBase<EmailConfirmation, EmailConfirmationId>(accountDbContext), IEmailConfirmationRepository
{
    public EmailConfirmation[] GetByEmail(string email)
    {
        return DbSet
            .Where(ec => !ec.Completed)
            .Where(ec => ec.Email == email.ToLowerInvariant())
            .ToArray();
    }
}
