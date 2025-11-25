namespace PlatformPlatform.SharedKernel.Domain;

public interface IBulkAddRepository<in T> where T : IAggregateRoot
{
    Task AddRangeAsync(IEnumerable<T> aggregates, CancellationToken cancellationToken);
}
