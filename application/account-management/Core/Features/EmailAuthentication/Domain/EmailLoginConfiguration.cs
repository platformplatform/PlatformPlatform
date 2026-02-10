using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlatformPlatform.SharedKernel.EntityFramework;

namespace PlatformPlatform.AccountManagement.Features.EmailAuthentication.Domain;

public sealed class EmailLoginConfiguration : IEntityTypeConfiguration<EmailLogin>
{
    public void Configure(EntityTypeBuilder<EmailLogin> builder)
    {
        builder.ToTable("EmailLogins");
        builder.MapStronglyTypedUuid<EmailLogin, EmailLoginId>(el => el.Id);
    }
}
