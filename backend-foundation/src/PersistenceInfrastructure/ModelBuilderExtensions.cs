using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PlatformPlatform.Foundation.DddCqrsFramework.Entities;
using PlatformPlatform.Foundation.DddCqrsFramework.Identity;

namespace PlatformPlatform.Foundation.PersistenceInfrastructure;

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