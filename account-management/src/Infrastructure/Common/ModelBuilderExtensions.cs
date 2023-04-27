using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlatformPlatform.AccountManagement.Domain.Primitives;

namespace PlatformPlatform.AccountManagement.Infrastructure.Common;

public static class ModelBuilderExtensions
{
    /// <summary>
    ///     This method is used to tell Entity Framework how to map a strongly typed ID to a SQL column using the
    ///     underlying type of the strongly-typed ID.
    /// </summary>
    public static void ConfigureStronglyTypedId<T, TId>(this EntityTypeBuilder<T> entity)
        where T : Entity<TId>
        where TId : StronglyTypedId<TId>
    {
        entity
            .Property(e => e.Id)
            .HasConversion(v => v.Value, v => (TId) Activator.CreateInstance(typeof(TId), v)!);
    }
}