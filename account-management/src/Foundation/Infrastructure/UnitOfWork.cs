using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using PlatformPlatform.Foundation.Application.Persistence;
using PlatformPlatform.Foundation.DddCore;

namespace PlatformPlatform.Foundation.Infrastructure;

/// <summary>
///     UnitOfWork is an implementation of the IUnitOfWork interface from the Domain layer. It is responsible for
///     committing any changes to the ApplicationDbContext and saving them to the database. The UnitOfWork is called
///     from the <see cref="UnitOfWorkPipelineBehavior{TRequest,TResponse}" /> in the Application layer.
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
        if (GetAggregatesWithDomainEvents().Any())
        {
            throw new InvalidOperationException("Domain events must be handled before committing the UnitOfWork.");
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public IEnumerable<IAggregateRoot> GetAggregatesWithDomainEvents()
    {
        return _dbContext.ChangeTracker
            .Entries<IAggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity);
    }
}