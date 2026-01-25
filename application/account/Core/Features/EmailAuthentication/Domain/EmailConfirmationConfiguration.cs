using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlatformPlatform.SharedKernel.EntityFramework;

namespace PlatformPlatform.Account.Features.EmailAuthentication.Domain;

public sealed class EmailConfirmationConfiguration : IEntityTypeConfiguration<EmailConfirmation>
{
    public void Configure(EntityTypeBuilder<EmailConfirmation> builder)
    {
        builder.MapStronglyTypedUuid<EmailConfirmation, EmailConfirmationId>(ec => ec.Id);
    }
}
