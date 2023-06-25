using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PlatformPlatform.SharedKernel.DomainCore.Identity;

namespace PlatformPlatform.SharedKernel.InfrastructureCore.EntityFramework;

public static class ModelBuilderExtensions
{
    /// <summary>
    ///     This method is used to tell Entity Framework how to map a strongly typed ID to a SQL column using the
    ///     underlying type of the strongly-typed ID.
    /// </summary>
    [UsedImplicitly]
    public static void MapStronglyTypedLongId<T, TId>(this ModelBuilder modelBuilder,
        Expression<Func<T, TId>> expression)
        where T : class where TId : StronglyTypedLongId<TId>
    {
        modelBuilder
            .Entity<T>()
            .Property(expression)
            .HasConversion(v => v.Value, v => (Activator.CreateInstance(typeof(TId), v) as TId)!);
    }

    public static void MapStronglyTypedUuid<T, TId>(this ModelBuilder modelBuilder, Expression<Func<T, TId>> expression)
        where T : class where TId : StronglyTypedUlid<TId>
    {
        modelBuilder
            .Entity<T>()
            .Property(expression)
            .HasConversion(v => v.Value, v => (Activator.CreateInstance(typeof(TId), v) as TId)!);
    }

    public static void MapStronglyTypedId<T, TId, TValue>(this ModelBuilder modelBuilder,
        Expression<Func<T, TId>> expression)
        where T : class
        where TValue : IComparable<TValue>
        where TId : StronglyTypedId<TValue, TId>
    {
        modelBuilder
            .Entity<T>()
            .Property(expression)
            .HasConversion(v => v.Value, v => (Activator.CreateInstance(typeof(TId), v) as TId)!);
    }

    /// <summary>
    ///     This method is used to tell Entity Framework to store all enum properties as strings in the database.
    /// </summary>
    [UsedImplicitly]
    public static ModelBuilder UseStringForEnums(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (!property.ClrType.IsEnum) continue;

                var converterType = typeof(EnumToStringConverter<>).MakeGenericType(property.ClrType);
                var converterInstance = (ValueConverter) Activator.CreateInstance(converterType)!;
                property.SetValueConverter(converterInstance);
            }
        }

        return modelBuilder;
    }
}