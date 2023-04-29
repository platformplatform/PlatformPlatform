using PlatformPlatform.AccountManagement.Application.Shared.Behaviors;
using PlatformPlatform.AccountManagement.Domain.Shared;

namespace PlatformPlatform.AccountManagement.Infrastructure.Shared;

/// <summary>
///     UnitOfWork is an implementation of the IUnitOfWork interface from the Domain layer.
///     It is responsible for committing any changes to the ApplicationDbContext and saving them to the database.
///     This class is called from the <see cref="UnitOfWorkBehavior{TRequest,TResponse}" /> in the Application layer.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _applicationDbContext;

    public UnitOfWork(ApplicationDbContext applicationDbContext)
    {
        _applicationDbContext = applicationDbContext;
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        await _applicationDbContext.SaveChangesAsync(cancellationToken);
    }
}