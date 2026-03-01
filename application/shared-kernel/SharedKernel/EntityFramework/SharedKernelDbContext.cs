using System.Linq.Expressions;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PlatformPlatform.SharedKernel.Domain;
using PlatformPlatform.SharedKernel.ExecutionContext;

namespace PlatformPlatform.SharedKernel.EntityFramework;

/// <summary>
///     The SharedKernelDbContext class represents the Entity Framework Core DbContext for managing data access to the
///     database, like creation, querying, and updating of <see cref="IAggregateRoot" /> entities.
/// </summary>
public abstract class SharedKernelDbContext<TContext>(DbContextOptions<TContext> options, IExecutionContext executionContext, TimeProvider timeProvider)
    : DbContext(options), ITimeProviderSource, IPurgeTracker where TContext : DbContext
{
    private readonly HashSet<object> _entitiesMarkedForPurge = [];

    protected TenantId? TenantId => executionContext.TenantId;

    void IPurgeTracker.MarkForPurge(object entity)
    {
        _entitiesMarkedForPurge.Add(entity);
    }

    bool IPurgeTracker.IsMarkedForPurge(object entity)
    {
        return _entitiesMarkedForPurge.Contains(entity);
    }

    public TimeProvider TimeProvider => timeProvider;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        optionsBuilder.AddInterceptors(new UpdateAuditableEntitiesInterceptor(), new SoftDeleteInterceptor());
        optionsBuilder.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));

        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TContext).Assembly);

        // Set pluralized table names for all aggregates
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var tableNameAnnotation = entityType.GetAnnotations().FirstOrDefault(a => a.Name == "Relational:TableName");
            if (tableNameAnnotation?.Value is not null)
            {
                entityType.SetTableName(tableNameAnnotation.Value.ToString());
            }
            else
            {
                var tableName = entityType.GetTableName()!.Pluralize();
                entityType.SetTableName(tableName);
            }
        }

        // Ensures that all enum properties are stored as strings in the database.
        modelBuilder.UseStringForEnums();

        ApplyNamedQueryFilters(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    ///     Applies named query filters to entities implementing <see cref="ITenantScopedEntity" /> and
    ///     <see cref="ISoftDeletable" /> interfaces. Named filters can be selectively disabled at query time
    ///     using IgnoreQueryFilters(["FilterName"]).
    /// </summary>
    private void ApplyNamedQueryFilters(ModelBuilder modelBuilder)
    {
        var entityTypes = modelBuilder.Model.GetEntityTypes()
            .Where(t => !t.IsOwned())
            .ToList();

        foreach (var entityType in entityTypes)
        {
            var clrType = entityType.ClrType;
            var parameter = Expression.Parameter(clrType, "entity");

            if (typeof(ITenantScopedEntity).IsAssignableFrom(clrType))
            {
                var tenantIdProperty = Expression.Property(parameter, nameof(ITenantScopedEntity.TenantId));
                var tenantIdValue = Expression.Property(Expression.Constant(this), nameof(TenantId));

                var condition = Expression.AndAlso(
                    Expression.NotEqual(tenantIdValue, Expression.Constant(null, typeof(TenantId))),
                    Expression.Equal(tenantIdProperty, tenantIdValue)
                );

                var lambda = Expression.Lambda(condition, parameter);
                modelBuilder.Entity(clrType).HasQueryFilter(QueryFilterNames.Tenant, lambda);
            }

            if (typeof(ISoftDeletable).IsAssignableFrom(clrType))
            {
                var deletedAtProperty = Expression.Property(parameter, nameof(ISoftDeletable.DeletedAt));
                var condition = Expression.Equal(deletedAtProperty, Expression.Constant(null, typeof(DateTimeOffset?)));

                var lambda = Expression.Lambda(condition, parameter);
                modelBuilder.Entity(clrType).HasQueryFilter(QueryFilterNames.SoftDelete, lambda);
            }
        }
    }
}
