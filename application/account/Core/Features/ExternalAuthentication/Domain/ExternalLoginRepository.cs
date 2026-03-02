using Account.Database;
using SharedKernel.Domain;
using SharedKernel.Persistence;

namespace Account.Features.ExternalAuthentication.Domain;

public interface IExternalLoginRepository : IAppendRepository<ExternalLogin, ExternalLoginId>
{
    void Update(ExternalLogin aggregate);
}

public sealed class ExternalLoginRepository(AccountDbContext accountDbContext)
    : RepositoryBase<ExternalLogin, ExternalLoginId>(accountDbContext), IExternalLoginRepository;
