using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedKernel.EntityFramework;

namespace Account.Features.EmailAuthentication.Domain;

public sealed class EmailLoginConfiguration : IEntityTypeConfiguration<EmailLogin>
{
    public void Configure(EntityTypeBuilder<EmailLogin> builder)
    {
        builder.ToTable("EmailLogins");
        builder.MapStronglyTypedUuid<EmailLogin, EmailLoginId>(el => el.Id);
    }
}
