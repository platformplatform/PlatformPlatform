using Microsoft.EntityFrameworkCore;
using PlatformPlatform.AccountManagement.Database;
using PlatformPlatform.AccountManagement.Features.Teams.Domain;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.Persistence;

namespace PlatformPlatform.AccountManagement.Features.TeamMembers.Domain;

public interface ITeamMemberRepository : ICrudRepository<TeamMember, TeamMemberId>, IBulkRemoveRepository<TeamMember>
{
    Task<TeamMember?> GetByTeamAndUserAsync(TeamId teamId, UserId userId, CancellationToken cancellationToken);

    Task<TeamMember[]> GetByTeamIdAsync(TeamId teamId, CancellationToken cancellationToken);

    Task<TeamMember[]> GetByIdsAsync(TeamMemberId[] ids, CancellationToken cancellationToken);

    Task<bool> IsUserTeamAdminAsync(TeamId teamId, UserId userId, CancellationToken cancellationToken);

    Task BulkAddAsync(TeamMember[] members, CancellationToken cancellationToken);

    Task<TeamMember[]> GetAllAsync(CancellationToken cancellationToken);
}

public sealed class TeamMemberRepository(AccountManagementDbContext accountManagementDbContext)
    : RepositoryBase<TeamMember, TeamMemberId>(accountManagementDbContext), ITeamMemberRepository
{
    public async Task<TeamMember?> GetByTeamAndUserAsync(TeamId teamId, UserId userId, CancellationToken cancellationToken)
    {
        return await DbSet
            .SingleOrDefaultAsync(tm => tm.TeamId == teamId && tm.UserId == userId, cancellationToken);
    }

    public async Task<TeamMember[]> GetByTeamIdAsync(TeamId teamId, CancellationToken cancellationToken)
    {
        return await DbSet.Where(tm => tm.TeamId == teamId).ToArrayAsync(cancellationToken);
    }

    public async Task<TeamMember[]> GetByIdsAsync(TeamMemberId[] ids, CancellationToken cancellationToken)
    {
        return await DbSet.Where(tm => ids.Contains(tm.Id)).ToArrayAsync(cancellationToken);
    }

    public async Task<bool> IsUserTeamAdminAsync(TeamId teamId, UserId userId, CancellationToken cancellationToken)
    {
        return await DbSet.AnyAsync(
            tm => tm.TeamId == teamId && tm.UserId == userId && tm.Role == TeamMemberRole.Admin,
            cancellationToken
        );
    }

    public async Task BulkAddAsync(TeamMember[] members, CancellationToken cancellationToken)
    {
        await DbSet.AddRangeAsync(members, cancellationToken);
    }

    public async Task<TeamMember[]> GetAllAsync(CancellationToken cancellationToken)
    {
        return await DbSet.ToArrayAsync(cancellationToken);
    }
}
