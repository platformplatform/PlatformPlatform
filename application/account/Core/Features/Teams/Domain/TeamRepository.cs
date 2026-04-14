using Account.Database;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
using SharedKernel.Persistence;

namespace Account.Features.Teams.Domain;

public interface ITeamRepository : ICrudRepository<Team, TeamId>
{
    Task<bool> IsNameUniqueAsync(string name, CancellationToken cancellationToken);

    Task<Team[]> GetAllAsync(CancellationToken cancellationToken);
}

public sealed class TeamRepository(AccountDbContext accountDbContext)
    : RepositoryBase<Team, TeamId>(accountDbContext), ITeamRepository
{
    public async Task<bool> IsNameUniqueAsync(string name, CancellationToken cancellationToken)
    {
        var lowerName = name.ToLower();
        return !await DbSet.AnyAsync(t => t.Name.ToLower() == lowerName, cancellationToken);
    }

    public async Task<Team[]> GetAllAsync(CancellationToken cancellationToken)
    {
        return await DbSet.OrderBy(t => t.Name).ToArrayAsync(cancellationToken);
    }
}
