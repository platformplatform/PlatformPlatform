using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using PlatformPlatform.AccountManagement.Domain.Tenants;
using PlatformPlatform.Foundation.DddCore.Entities;
using PlatformPlatform.Foundation.Infrastructure;

namespace PlatformPlatform.AccountManagement.Infrastructure;

/// <summary>
///     The ApplicationDbContext class represents the Entity Framework Core DbContext for managing data access to the
///     database, like creation, querying, and updating of <see cref="IAggregateRoot" /> entities.
/// </summary>
[SuppressMessage("Performance", "CA1822:Mark members as static")]
public sealed class ApplicationDbContext : FoundationDbContext<ApplicationDbContext>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ensures the strongly typed IDs can be saved and read by entity framework as the underlying type.
        modelBuilder.Entity<Tenant>().ConfigureStronglyTypedId<Tenant, TenantId>();
    }
}