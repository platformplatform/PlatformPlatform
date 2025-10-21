using Microsoft.EntityFrameworkCore;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Persistence;

namespace PlatformPlatform.AccountManagement.Features.Teams.Domain;

public interface ITeamRepository : ICrudRepository<Team, TeamId>
{
    Task<Team?> GetByNameAsync(string name, CancellationToken cancellationToken);

    Task<Team[]> GetAllAsync(CancellationToken cancellationToken);
}

public sealed class TeamRepository(AccountManagementDbContext accountManagementDbContext)
    : RepositoryBase<Team, TeamId>(accountManagementDbContext), ITeamRepository
{
    public async Task<Team?> GetByNameAsync(string name, CancellationToken cancellationToken)
    {
        return await DbSet.FirstOrDefaultAsync(t => t.Name == name, cancellationToken);
    }

    public async Task<Team[]> GetAllAsync(CancellationToken cancellationToken)
    {
        return await DbSet.ToArrayAsync(cancellationToken);
    }
}
