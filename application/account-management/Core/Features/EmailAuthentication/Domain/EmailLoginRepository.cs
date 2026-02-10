using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Persistence;

namespace PlatformPlatform.AccountManagement.Features.EmailAuthentication.Domain;

public interface IEmailLoginRepository : IAppendRepository<EmailLogin, EmailLoginId>
{
    void Update(EmailLogin aggregate);

    EmailLogin[] GetByEmail(string email);
}

public sealed class EmailLoginRepository(AccountManagementDbContext accountManagementDbContext)
    : RepositoryBase<EmailLogin, EmailLoginId>(accountManagementDbContext), IEmailLoginRepository
{
    public EmailLogin[] GetByEmail(string email)
    {
        return DbSet
            .Where(el => !el.Completed)
            .Where(el => el.Email == email.ToLowerInvariant())
            .ToArray();
    }
}
