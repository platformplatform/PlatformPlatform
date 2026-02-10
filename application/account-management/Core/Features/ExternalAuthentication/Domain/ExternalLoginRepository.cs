using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Persistence;

namespace PlatformPlatform.AccountManagement.Features.ExternalAuthentication.Domain;

public interface IExternalLoginRepository : IAppendRepository<ExternalLogin, ExternalLoginId>
{
    void Update(ExternalLogin aggregate);
}

public sealed class ExternalLoginRepository(AccountManagementDbContext accountManagementDbContext)
    : RepositoryBase<ExternalLogin, ExternalLoginId>(accountManagementDbContext), IExternalLoginRepository;
