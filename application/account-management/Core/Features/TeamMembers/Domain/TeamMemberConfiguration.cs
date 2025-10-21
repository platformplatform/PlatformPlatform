using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlatformPlatform.AccountManagement.Features.Teams.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.EntityFramework;

namespace PlatformPlatform.AccountManagement.Features.TeamMembers.Domain;

public sealed class TeamMemberConfiguration : IEntityTypeConfiguration<TeamMember>
{
    public void Configure(EntityTypeBuilder<TeamMember> builder)
    {
        builder.MapStronglyTypedUuid<TeamMember, TeamMemberId>(tm => tm.Id);
        builder.MapStronglyTypedUuid<TeamMember, TeamId>(tm => tm.TeamId);
        builder.MapStronglyTypedUuid<TeamMember, UserId>(tm => tm.UserId);
        builder.MapStronglyTypedLongId<TeamMember, TenantId>(tm => tm.TenantId);

        builder.Property(tm => tm.Role)
            .HasConversion<int>();

        builder.HasIndex(tm => new { tm.TeamId, tm.UserId }).IsUnique();
        builder.HasIndex(tm => tm.UserId);
        builder.HasIndex(tm => tm.TenantId);

        builder.HasOne<Team>()
            .WithMany()
            .HasForeignKey(tm => tm.TeamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(tm => tm.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
