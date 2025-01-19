using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PlatformPlatform.SharedKernel.StronglyTypedIds;

namespace PlatformPlatform.SharedKernel.EntityFramework;

public static class ModelBuilderExtensions
{
    /// <summary>
    ///     This method is used to tell Entity Framework how to map a strongly typed ID to a SQL column using the
    ///     underlying type of the strongly-typed ID.
    /// </summary>
    public static void MapStronglyTypedLongId<T, TId>(this EntityTypeBuilder<T> builder, Expression<Func<T, TId>> expression)
        where T : class where TId : StronglyTypedLongId<TId>
    {
        builder
            .Property(expression)
            .HasConversion(v => v.Value, v => (Activator.CreateInstance(typeof(TId), v) as TId)!);
    }

    public static void MapStronglyTypedUuid<T, TId>(this EntityTypeBuilder<T> builder, Expression<Func<T, TId>> expression)
        where T : class where TId : StronglyTypedUlid<TId>
    {
        builder
            .Property(expression)
            .HasConversion(v => v.Value, v => (Activator.CreateInstance(typeof(TId), v) as TId)!);
    }

    public static void MapStronglyTypedId<T, TId, TValue>(this EntityTypeBuilder<T> builder, Expression<Func<T, TId>> expression)
        where T : class
        where TValue : IComparable<TValue>
        where TId : StronglyTypedId<TValue, TId>
    {
        builder
            .Property(expression)
            .HasConversion(v => v.Value, v => (Activator.CreateInstance(typeof(TId), v) as TId)!);
    }

    public static void MapStronglyTypedNullableId<T, TId, TValue>(
        this EntityTypeBuilder<T> builder,
        Expression<Func<T, TId?>> idExpression
    )
        where T : class
        where TValue : class, IComparable<TValue>
        where TId : StronglyTypedId<TValue, TId>
    {
        var nullConstant = Expression.Constant(null, typeof(TValue));
        var idParameter = Expression.Parameter(typeof(TId), "id");
        var idValueProperty = Expression.Property(idParameter, nameof(StronglyTypedId<TValue, TId>.Value));
        var idCoalesceExpression =
            Expression.Lambda<Func<TId, TValue>>(Expression.Coalesce(idValueProperty, nullConstant), idParameter);

        builder
            .Property(idExpression)
            .HasConversion(idCoalesceExpression!, v => Activator.CreateInstance(typeof(TId), v) as TId);
    }

    /// <summary>
    ///     This method is used to tell Entity Framework to store all enum properties as strings in the database.
    /// </summary>
    public static ModelBuilder UseStringForEnums(this ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (!property.ClrType.IsEnum) continue;

                var converterType = typeof(EnumToStringConverter<>).MakeGenericType(property.ClrType);
                var converterInstance = (ValueConverter)Activator.CreateInstance(converterType)!;
                property.SetValueConverter(converterInstance);
            }
        }

        return modelBuilder;
    }
}
