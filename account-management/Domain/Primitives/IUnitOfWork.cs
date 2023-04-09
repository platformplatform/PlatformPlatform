namespace PlatformPlatform.AccountManagement.Domain.Primitives;

public interface IUnitOfWork
{
    Task CommitAsync(CancellationToken cancellationToken);
}