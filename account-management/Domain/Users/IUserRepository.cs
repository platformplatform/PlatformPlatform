using PlatformPlatform.SharedKernel.DomainCore.Persistence;

namespace PlatformPlatform.AccountManagement.Domain.Users;

public interface IUserRepository : IRepository<User, UserId>
{
    Task<bool> IsEmailFreeAsync(string email, CancellationToken cancellationToken);
}