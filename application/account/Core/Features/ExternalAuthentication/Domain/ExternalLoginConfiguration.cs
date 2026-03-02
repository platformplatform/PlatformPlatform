using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SharedKernel.EntityFramework;

namespace Account.Features.ExternalAuthentication.Domain;

public sealed class ExternalLoginConfiguration : IEntityTypeConfiguration<ExternalLogin>
{
    public void Configure(EntityTypeBuilder<ExternalLogin> builder)
    {
        builder.ToTable("ExternalLogins");
        builder.MapStronglyTypedId<ExternalLogin, ExternalLoginId, string>(el => el.Id);
    }
}
