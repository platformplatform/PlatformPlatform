namespace PlatformPlatform.AccountManagement.Domain.Primitives;

/// <summary>
///     IUnitOfWork interface provides a method for committing changes made within the unit of work.
///     A Unit of work is a logical transaction, that groups together changes that are related to each other.
///     The IUnitOfWork is implemented in the Infrastructure layer and is controlled by the UnitOfWorkBehavior in the
///     Application layer.
/// </summary>
public interface IUnitOfWork
{
    Task CommitAsync(CancellationToken cancellationToken);
}