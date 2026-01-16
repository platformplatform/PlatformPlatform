using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlatformPlatform.AccountManagement.Features.EmailConfirmations.Domain;
using PlatformPlatform.AccountManagement.Features.Users.Domain;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.EntityFramework;

namespace PlatformPlatform.AccountManagement.Features.Authentication.Domain;

public sealed class EmailLoginConfiguration : IEntityTypeConfiguration<EmailLogin>
{
    public void Configure(EntityTypeBuilder<EmailLogin> builder)
    {
        builder.ToTable("EmailLogins");
        builder.MapStronglyTypedId<EmailLogin, EmailLoginId, string>(el => el.Id);
        builder.MapStronglyTypedLongId<EmailLogin, TenantId>(el => el.TenantId);
        builder.MapStronglyTypedUuid<EmailLogin, UserId>(el => el.UserId);
        builder.MapStronglyTypedUuid<EmailLogin, EmailConfirmationId>(el => el.EmailConfirmationId);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(el => el.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
