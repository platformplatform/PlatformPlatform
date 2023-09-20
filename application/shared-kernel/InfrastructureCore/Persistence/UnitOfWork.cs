using Microsoft.EntityFrameworkCore;
using PlatformPlatform.SharedKernel.ApplicationCore.Behaviors;
using PlatformPlatform.SharedKernel.DomainCore.Entities;
using PlatformPlatform.SharedKernel.DomainCore.Persistence;

namespace PlatformPlatform.SharedKernel.InfrastructureCore.Persistence;

/// <summary>
///     UnitOfWork is an implementation of the IUnitOfWork interface from the Domain layer. It is responsible for
///     committing any changes to the application specific DbContext and saving them to the database. The UnitOfWork is
///     called from the <see cref="UnitOfWorkPipelineBehavior{TRequest,TResponse}" /> in the Application layer.
/// </summary>
[UsedImplicitly]
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly DbContext _dbContext;

    public UnitOfWork(DbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        if (_dbContext.ChangeTracker.Entries<IAggregateRoot>().Any(e => e.Entity.DomainEvents.Any()))
        {
            throw new InvalidOperationException("Domain events must be handled before committing the UnitOfWork.");
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}