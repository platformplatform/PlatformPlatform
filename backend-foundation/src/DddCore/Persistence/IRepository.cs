using JetBrains.Annotations;
using PlatformPlatform.Foundation.DddCore.Entities;

namespace PlatformPlatform.Foundation.DddCore.Persistence;

/// <summary>
///     IRepository is a generic interface for repositories. Repositories are a DDD concept, and are used
///     as an abstraction over the database. In DDD, repositories are used to persist <see cref="IAggregateRoot" />.
///     <see cref="Entity{T}" /> that are not aggregates cannot be fetched from the database using a repository. They
///     must be fetched indirectly by fetching the aggregate that they belong to. E.g., to fetch an OrderLine, you must
///     fetch the Order that it belongs to.
///     Repositories are not responsible for commiting the changes to the database, which is handled by the
///     <see cref="IUnitOfWork" />. What this means is that when you add, update, or delete aggregates, they are just
///     marked to be added, updated, or deleted, and it's not until the <see cref="IUnitOfWork" /> is committed that the
///     changes are actually persisted to the database.
/// </summary>
public interface IRepository<T, in TId> where T : IAggregateRoot where TId : IComparable<TId>
{
    Task<T?> GetByIdAsync(TId id, CancellationToken cancellationToken);

    void Add(T aggregate);

    [UsedImplicitly]
    void Update(T aggregate);

    [UsedImplicitly]
    void Remove(T aggregate);
}