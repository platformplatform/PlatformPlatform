using Account.Features.Tenants.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedKernel.Domain;
using SharedKernel.EntityFramework;

namespace Account.Features.Teams.Domain;

public sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.MapStronglyTypedUuid<Team, TeamId>(t => t.Id);
        builder.MapStronglyTypedLongId<Team, TenantId>(t => t.TenantId);
        builder
            .HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(t => t.TenantId)
            .HasPrincipalKey(t => t.Id);
    }
}
