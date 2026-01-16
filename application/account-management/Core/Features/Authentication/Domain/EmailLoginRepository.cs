using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Persistence;

namespace PlatformPlatform.AccountManagement.Features.Authentication.Domain;

public interface IEmailLoginRepository : IAppendRepository<EmailLogin, EmailLoginId>
{
    void Update(EmailLogin aggregate);
}

public sealed class EmailLoginRepository(AccountManagementDbContext accountManagementDbContext)
    : RepositoryBase<EmailLogin, EmailLoginId>(accountManagementDbContext), IEmailLoginRepository;
