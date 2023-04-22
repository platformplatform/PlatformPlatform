namespace PlatformPlatform.AccountManagement.Domain.Primitives;

/// <summary>
///     IRepository is a generic interface for repositories. Repositories are a DDD concept, and are used
///     as an abstraction over the database. In DDD, repositories are used to persist <see cref="IAggregateRoot" />.
///     <see cref="Entity{T}" /> that are not aggregates cannot be fetched from the database using a repository.
///     Repositories are not responsible for saving the entities to the database, which is handled by the
///     <see cref="IUnitOfWork" />.
/// </summary>
public interface IRepository<T, in TId> where T : IAggregateRoot where TId : IComparable<TId>
{
    Task<T?> GetByIdAsync(TId id, CancellationToken cancellationToken);

    Task AddAsync(T tenant, CancellationToken cancellationToken);
}