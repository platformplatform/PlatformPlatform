using Microsoft.EntityFrameworkCore;
using PlatformPlatform.AccountManagement.Domain.Shared;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.AccountManagement.Infrastructure.Shared;

namespace PlatformPlatform.AccountManagement.Infrastructure;

/// <summary>
///     The ApplicationDbContext class represents the Entity Framework Core DbContext for managing data access to the
///     database, like creation, querying, and updating of <see cref="IAggregateRoot" /> entities.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Ensures the strongly typed IDs can be saved and read by entity framework as the underlying type.
        modelBuilder.Entity<Tenant>().ConfigureStronglyTypedId<Tenant, TenantId>();
    }

    /// <summary>
    ///     This method is called when the DbContext is being configured, allowing for customizations
    ///     that can affect the behavior of the database connection, caching, or query execution.
    ///     E.g., interceptors are classes that can handle or modify various events during the lifetime of a DbContext,
    ///     such as executing commands, reading results, or committing transactions.
    /// </summary>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(new UpdateAuditableEntitiesInterceptor());
    }
}