using Account.Database;
using SharedKernel.Domain;
using SharedKernel.Persistence;

namespace Account.Features.EmailAuthentication.Domain;

public interface IEmailLoginRepository : IAppendRepository<EmailLogin, EmailLoginId>
{
    void Update(EmailLogin aggregate);

    EmailLogin[] GetByEmail(string email);
}

public sealed class EmailLoginRepository(AccountDbContext accountDbContext)
    : RepositoryBase<EmailLogin, EmailLoginId>(accountDbContext), IEmailLoginRepository
{
    public EmailLogin[] GetByEmail(string email)
    {
        return DbSet
            .Where(el => !el.Completed)
            .Where(el => el.Email == email.ToLowerInvariant())
            .ToArray();
    }
}
