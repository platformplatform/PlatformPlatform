namespace PlatformPlatform.AccountManagement.Domain.Primitives;

public interface IRepository<T, in TId> where T : IAggregateRoot where TId : IComparable
{
    Task<T?> GetByIdAsync(TId id, CancellationToken cancellationToken);

    Task AddAsync(T tenant, CancellationToken cancellationToken);
}