using PlatformPlatform.AccountManagement.Domain.Authentication;
using PlatformPlatform.SharedKernel.InfrastructureCore.Persistence;

namespace PlatformPlatform.AccountManagement.Infrastructure.Authentication;

public sealed class LoginRepository(AccountManagementDbContext accountManagementDbContext)
    : RepositoryBase<Login, LoginId>(accountManagementDbContext), ILoginRepository;
