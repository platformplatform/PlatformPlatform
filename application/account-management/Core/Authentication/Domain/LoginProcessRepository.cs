using PlatformPlatform.AccountManagement.Core.Database;
using PlatformPlatform.SharedKernel.InfrastructureCore.Persistence;

namespace PlatformPlatform.AccountManagement.Core.Authentication.Domain;

public sealed class LoginRepository(AccountManagementDbContext accountManagementDbContext)
    : RepositoryBase<Login, LoginId>(accountManagementDbContext), ILoginRepository;
