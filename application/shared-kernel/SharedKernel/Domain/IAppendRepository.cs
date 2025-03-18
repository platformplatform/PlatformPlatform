namespace PlatformPlatform.SharedKernel.Domain;

public interface IAppendRepository<T, in TId> where T : IAggregateRoot
{
    Task<T?> GetByIdAsync(TId id, CancellationToken cancellationToken);

    Task AddAsync(T aggregate, CancellationToken cancellationToken);
}
