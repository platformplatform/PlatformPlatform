using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using PlatformPlatform.AccountManagement.Domain.Users;
using PlatformPlatform.SharedKernel.InfrastructureCore.Persistence;

namespace PlatformPlatform.AccountManagement.Infrastructure.Users;

[UsedImplicitly]
internal sealed class UserRepository : RepositoryBase<User, UserId>, IUserRepository
{
    public UserRepository(AccountManagementDbContext accountManagementDbContext) : base(accountManagementDbContext)
    {
    }

    public Task<bool> IsEmailFreeAsync(string email, CancellationToken cancellationToken)
    {
        return DbSet.AllAsync(user => user.Email != email, cancellationToken);
    }
}