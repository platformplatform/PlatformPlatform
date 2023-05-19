using PlatformPlatform.Foundation.DddCqrsFramework.Entities;

namespace PlatformPlatform.Foundation.DddCqrsFramework.Persistence;

/// <summary>
///     IUnitOfWork interface provides a method for committing changes made within the unit of work.
///     A unit of work is a logical transaction that groups together changes that are related to each other.
///     The IUnitOfWork is implemented in the Infrastructure layer and is controlled by the UnitOfWorkPipelineBehavior
///     in the Application layer.
///     Use <see cref="IRepository{T,TId}" /> to add, update, and delete aggregates to a unit of work. When the unit of
///     work is committed, the changes are persisted to the database.
/// </summary>
public interface IUnitOfWork
{
    Task CommitAsync(CancellationToken cancellationToken);

    IEnumerable<IAggregateRoot> GetAggregatesWithDomainEvents();
}