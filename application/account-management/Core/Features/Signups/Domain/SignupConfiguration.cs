using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.EntityFramework;

namespace PlatformPlatform.AccountManagement.Features.Signups.Domain;

public sealed class SignupConfiguration : IEntityTypeConfiguration<Signup>
{
    public void Configure(EntityTypeBuilder<Signup> builder)
    {
        builder.MapStronglyTypedUuid<Signup, SignupId>(a => a.Id);
        builder.MapStronglyTypedNullableId<Signup, TenantId, string>(u => u.TenantId);
    }
}
