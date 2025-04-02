using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlatformPlatform.AccountManagement.Features.EmailConfirmations.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.EntityFramework;

namespace PlatformPlatform.AccountManagement.Features.Authentication.Domain;

public sealed class LoginConfiguration : IEntityTypeConfiguration<Login>
{
    public void Configure(EntityTypeBuilder<Login> builder)
    {
        builder.MapStronglyTypedId<Login, LoginId, string>(l => l.Id);
        builder.MapStronglyTypedLongId<Login, TenantId>(l => l.TenantId);
        builder.MapStronglyTypedUuid<Login, UserId>(l => l.UserId);
        builder.MapStronglyTypedUuid<Login, EmailConfirmationId>(l => l.EmailConfirmationId);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
