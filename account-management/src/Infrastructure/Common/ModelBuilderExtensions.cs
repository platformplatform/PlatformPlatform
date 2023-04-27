using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlatformPlatform.AccountManagement.Domain.Primitives;

namespace PlatformPlatform.AccountManagement.Infrastructure.Common;

/// <summary>
///     ModelBuilderExtensions contains helper methods for configuring Entity Framework Core ModelBuilder.
/// </summary>
public static class ModelBuilderExtensions
{
    /// <summary>
    ///     Configures the EntityTypeBuilder for an entity with a strongly-typed ID.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <typeparam name="TId">The type of the strongly-typed ID.</typeparam>
    /// <param name="entity">The EntityTypeBuilder to be configured.</param>
    public static void ConfigureStronglyTypedId<T, TId>(this EntityTypeBuilder<T> entity)
        where T : Entity<TId>
        where TId : StronglyTypedId<TId>
    {
        entity
            .Property(e => e.Id)
            .HasConversion(v => v.Value, v => (TId) Activator.CreateInstance(typeof(TId), v)!);
    }
}