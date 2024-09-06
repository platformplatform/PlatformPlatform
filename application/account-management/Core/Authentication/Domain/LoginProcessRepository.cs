using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.SharedKernel.Entities;
using PlatformPlatform.SharedKernel.Persistence;

namespace PlatformPlatform.AccountManagement.Authentication.Domain;

public interface ILoginRepository : ICrudRepository<Login, LoginId>;

public sealed class LoginRepository(AccountManagementDbContext accountManagementDbContext)
    : RepositoryBase<Login, LoginId>(accountManagementDbContext), ILoginRepository;
