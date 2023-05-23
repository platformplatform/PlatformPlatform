using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using PlatformPlatform.Foundation.DomainModeling.Entities;

namespace PlatformPlatform.Foundation.InfrastructureCore.EntityFramework;

/// <summary>
///     The FoundationDbContext class represents the Entity Framework Core DbContext for managing data access to the
///     database, like creation, querying, and updating of <see cref="IAggregateRoot" /> entities.
/// </summary>
public abstract class FoundationDbContext<TContext> : DbContext where TContext : DbContext
{
    protected FoundationDbContext(DbContextOptions<TContext> options) : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        optionsBuilder.AddInterceptors(new UpdateAuditableEntitiesInterceptor());

        var serviceProvider = optionsBuilder.Options.FindExtension<CoreOptionsExtension>()?.ApplicationServiceProvider;
        if (serviceProvider != null)
        {
            var entityValidationInterceptor = serviceProvider.GetService<EntityValidationSaveChangesInterceptor>();
            if (entityValidationInterceptor != null)
            {
                optionsBuilder.AddInterceptors(entityValidationInterceptor);
            }
        }

        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Ensures that all enum properties are stored as strings in the database.
        modelBuilder.UseStringForEnums();

        base.OnModelCreating(modelBuilder);
    }
}