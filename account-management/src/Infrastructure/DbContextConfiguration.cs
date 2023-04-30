using Microsoft.EntityFrameworkCore;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.AccountManagement.Infrastructure.Shared;

namespace PlatformPlatform.AccountManagement.Infrastructure;

public static class DbContextConfiguration
{
    public static void ConfigureOnModelCreating(ModelBuilder modelBuilder)
    {
        // Ensures that all enum properties are stored as strings in the database.
        modelBuilder.UseStringForEnums();

        // Ensures the strongly typed IDs can be saved and read by entity framework as the underlying type.
        modelBuilder.Entity<Tenant>().ConfigureStronglyTypedId<Tenant, TenantId>();
    }

    /// <summary>
    ///     This method is called when the DbContext is being configured, allowing for customizations
    ///     that can affect the behavior of the database connection, caching, or query execution.
    ///     E.g., interceptors are classes that can handle or modify various events during the lifetime of a DbContext,
    ///     such as executing commands, reading results, or committing transactions.
    /// </summary>
    public static void ConfigureOnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(new UpdateAuditableEntitiesInterceptor());
    }
}