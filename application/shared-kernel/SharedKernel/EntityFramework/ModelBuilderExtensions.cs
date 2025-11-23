using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PlatformPlatform.SharedKernel.StronglyTypedIds;

namespace PlatformPlatform.SharedKernel.EntityFramework;

public static class ModelBuilderExtensions
{
    extension<T>(EntityTypeBuilder<T> builder) where T : class
    {
        /// <summary>
        ///     This method is used to tell Entity Framework how to map a strongly typed ID to a SQL column using the
        ///     underlying type of the strongly-typed ID.
        /// </summary>
        public void MapStronglyTypedLongId<TId>(Expression<Func<T, TId>> expression) where TId : StronglyTypedLongId<TId>
        {
            builder
                .Property(expression)
                .HasConversion(v => v.Value, v => (Activator.CreateInstance(typeof(TId), v) as TId)!);
        }

        public void MapStronglyTypedUuid<TId>(Expression<Func<T, TId>> expression) where TId : StronglyTypedUlid<TId>
        {
            builder
                .Property(expression)
                .HasConversion(v => v.Value, v => (Activator.CreateInstance(typeof(TId), v) as TId)!);
        }

        public void MapStronglyTypedId<TId, TValue>(Expression<Func<T, TId>> expression)
            where TValue : IComparable<TValue>
            where TId : StronglyTypedId<TValue, TId>
        {
            builder
                .Property(expression)
                .HasConversion(v => v.Value, v => (Activator.CreateInstance(typeof(TId), v) as TId)!);
        }

        public void MapStronglyTypedNullableId<TId, TValue>(
            Expression<Func<T, TId?>> idExpression
        )
            where TValue : class, IComparable<TValue>
            where TId : StronglyTypedId<TValue, TId>
        {
            var nullConstant = Expression.Constant(null, typeof(TValue));
            var idParameter = Expression.Parameter(typeof(TId), "id");
            var idValueProperty = Expression.Property(idParameter, "Value");
            var idCoalesceExpression =
                Expression.Lambda<Func<TId, TValue>>(Expression.Coalesce(idValueProperty, nullConstant), idParameter);

            builder
                .Property(idExpression)
                .HasConversion(idCoalesceExpression!, v => Activator.CreateInstance(typeof(TId), v) as TId);
        }
    }

    extension(ModelBuilder modelBuilder)
    {
        /// <summary>
        ///     This method is used to tell Entity Framework to store all enum properties as strings in the database.
        /// </summary>
        public ModelBuilder UseStringForEnums()
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
}
