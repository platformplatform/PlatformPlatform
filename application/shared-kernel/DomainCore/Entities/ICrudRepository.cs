namespace PlatformPlatform.SharedKernel.DomainCore.Entities;

public interface ICrudRepository<T, in TId> where T : IAggregateRoot
{
    Task<T?> GetByIdAsync(TId id, CancellationToken cancellationToken);
    
    Task AddAsync(T aggregate, CancellationToken cancellationToken);
    
    void Update(T aggregate);
    
    void Remove(T aggregate);
}
