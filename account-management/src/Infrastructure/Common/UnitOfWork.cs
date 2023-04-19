using PlatformPlatform.AccountManagement.Domain.Primitives;

namespace PlatformPlatform.AccountManagement.Infrastructure.Common;

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