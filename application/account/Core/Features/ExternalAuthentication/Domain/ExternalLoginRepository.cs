using PlatformPlatform.Account.Database;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Persistence;

namespace PlatformPlatform.Account.Features.ExternalAuthentication.Domain;

public interface IExternalLoginRepository : IAppendRepository<ExternalLogin, ExternalLoginId>
{
    void Update(ExternalLogin aggregate);
}

public sealed class ExternalLoginRepository(AccountDbContext accountDbContext)
    : RepositoryBase<ExternalLogin, ExternalLoginId>(accountDbContext), IExternalLoginRepository;
