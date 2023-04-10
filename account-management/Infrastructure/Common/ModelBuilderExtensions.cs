using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PlatformPlatform.AccountManagement.Infrastructure.Common;

public static class ModelBuilderExtensions
{
    public static EntityTypeBuilder<T> ConfigureStronglyTypedId<T, TId>(this EntityTypeBuilder<T> entity)
        where T : Entity<TId>
        where TId : StronglyTypedId<TId>
    {
        entity
            .Property(e => e.Id)
            .HasConversion(v => v.Value, v => (TId) Activator.CreateInstance(typeof(TId), v)!);

        return entity;
    }
}