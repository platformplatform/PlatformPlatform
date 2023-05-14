using JetBrains.Annotations;
using PlatformPlatform.AccountManagement.Application.Shared.Persistence;
using PlatformPlatform.AccountManagement.Domain.Shared;

namespace PlatformPlatform.AccountManagement.Infrastructure.Shared;

/// <summary>
///     UnitOfWork is an implementation of the IUnitOfWork interface from the Domain layer. It is responsible for
///     committing any changes to the ApplicationDbContext and saving them to the database. The UnitOfWork is called
///     from the <see cref="UnitOfWorkPipelineBehavior{TRequest,TResponse}" /> in the Application layer.
/// </summary>
[UsedImplicitly]
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _applicationDbContext;

    public UnitOfWork(ApplicationDbContext applicationDbContext)
    {
        _applicationDbContext = applicationDbContext;
    }

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        if (GetAggregatesWithDomainEvents().Any())
        {
            throw new InvalidOperationException("Domain events must be handled before committing the UnitOfWork.");
        }

        await _applicationDbContext.SaveChangesAsync(cancellationToken);
    }

    public IEnumerable<IAggregateRoot> GetAggregatesWithDomainEvents()
    {
        return _applicationDbContext.ChangeTracker
            .Entries<IAggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity);
    }
}