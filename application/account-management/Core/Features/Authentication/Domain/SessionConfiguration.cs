using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Authentication.TokenGeneration;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.EntityFramework;

namespace PlatformPlatform.AccountManagement.Features.Authentication.Domain;

public sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.MapStronglyTypedUuid<Session, SessionId>(s => s.Id);
        builder.MapStronglyTypedLongId<Session, TenantId>(s => s.TenantId);
        builder.MapStronglyTypedUuid<Session, UserId>(s => s.UserId);
        builder.MapStronglyTypedUuid<Session, RefreshTokenJti>(s => s.RefreshTokenJti);
        builder.Property(s => s.PreviousRefreshTokenJti)
            .HasConversion(v => v != null ? v.Value : null, v => v != null ? new RefreshTokenJti(v) : null);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
