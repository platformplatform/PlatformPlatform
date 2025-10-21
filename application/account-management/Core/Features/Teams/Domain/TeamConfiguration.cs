using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.EntityFramework;

namespace PlatformPlatform.AccountManagement.Features.Teams.Domain;

public sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.MapStronglyTypedUuid<Team, TeamId>(t => t.Id);
        builder.MapStronglyTypedLongId<Team, TenantId>(t => t.TenantId);

        builder.HasIndex(t => new { t.TenantId, t.Name }).IsUnique();
    }
}
