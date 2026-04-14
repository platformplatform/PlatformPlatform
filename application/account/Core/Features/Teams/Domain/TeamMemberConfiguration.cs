using Account.Features.Users.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedKernel.Domain;
using SharedKernel.EntityFramework;

namespace Account.Features.Teams.Domain;

public sealed class TeamMemberConfiguration : IEntityTypeConfiguration<TeamMember>
{
    public void Configure(EntityTypeBuilder<TeamMember> builder)
    {
        builder.MapStronglyTypedUuid<TeamMember, TeamMemberId>(tm => tm.Id);
        builder.MapStronglyTypedLongId<TeamMember, TenantId>(tm => tm.TenantId);
        builder.MapStronglyTypedUuid<TeamMember, TeamId>(tm => tm.TeamId);
        builder.MapStronglyTypedUuid<TeamMember, UserId>(tm => tm.UserId);

        builder
            .HasOne<Team>()
            .WithMany()
            .HasForeignKey(tm => tm.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(tm => tm.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
