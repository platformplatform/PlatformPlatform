using PlatformPlatform.Account.Database;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Persistence;

namespace PlatformPlatform.Account.Features.EmailAuthentication.Domain;

public interface IEmailLoginRepository : IAppendRepository<EmailLogin, EmailLoginId>
{
    void Update(EmailLogin aggregate);
}

public sealed class EmailLoginRepository(AccountDbContext accountDbContext)
    : RepositoryBase<EmailLogin, EmailLoginId>(accountDbContext), IEmailLoginRepository;
