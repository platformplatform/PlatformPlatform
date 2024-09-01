using PlatformPlatform.AccountManagement.Core.Database;
using PlatformPlatform.SharedKernel.Entities;
using PlatformPlatform.SharedKernel.Persistence;

namespace PlatformPlatform.AccountManagement.Core.Authentication.Domain;

public interface ILoginRepository : ICrudRepository<Login, LoginId>;

public sealed class LoginRepository(AccountManagementDbContext accountManagementDbContext)
    : RepositoryBase<Login, LoginId>(accountManagementDbContext), ILoginRepository;
