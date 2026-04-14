using Account.Database;
using Microsoft.EntityFrameworkCore;
using SharedKernel.Domain;
using SharedKernel.Persistence;

namespace Account.Features.Teams.Domain;

public interface ITeamMemberRepository : ICrudRepository<TeamMember, TeamMemberId>
{
    Task<TeamMember[]> GetByTeamIdAsync(TeamId teamId, CancellationToken cancellationToken);

    Task<TeamMember?> GetByTeamAndUserIdAsync(TeamId teamId, UserId userId, CancellationToken cancellationToken);

    Task<bool> IsUserMemberOfTeamAsync(TeamId teamId, UserId userId, CancellationToken cancellationToken);

    Task<bool> IsUserAdminOfTeamAsync(TeamId teamId, UserId userId, CancellationToken cancellationToken);
}

public sealed class TeamMemberRepository(AccountDbContext accountDbContext)
    : RepositoryBase<TeamMember, TeamMemberId>(accountDbContext), ITeamMemberRepository
{
    public async Task<TeamMember[]> GetByTeamIdAsync(TeamId teamId, CancellationToken cancellationToken)
    {
        return await DbSet.Where(tm => tm.TeamId == teamId).ToArrayAsync(cancellationToken);
    }

    public async Task<TeamMember?> GetByTeamAndUserIdAsync(TeamId teamId, UserId userId, CancellationToken cancellationToken)
    {
        return await DbSet.FirstOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == userId, cancellationToken);
    }

    public async Task<bool> IsUserMemberOfTeamAsync(TeamId teamId, UserId userId, CancellationToken cancellationToken)
    {
        return await DbSet.AnyAsync(tm => tm.TeamId == teamId && tm.UserId == userId, cancellationToken);
    }

    public async Task<bool> IsUserAdminOfTeamAsync(TeamId teamId, UserId userId, CancellationToken cancellationToken)
    {
        return await DbSet.AnyAsync(tm => tm.TeamId == teamId && tm.UserId == userId && tm.Role == TeamMemberRole.Admin, cancellationToken);
    }
}
