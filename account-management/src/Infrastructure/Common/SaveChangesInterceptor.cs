using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PlatformPlatform.AccountManagement.Domain.Primitives;

namespace PlatformPlatform.AccountManagement.Infrastructure.Common;

/// <summary>
///     The UpdateAuditableEntitiesInterceptor is a SaveChangesInterceptor that updates the ModifiedAt property
///     for IAuditableEntity instances when changes are made to the database.
/// </summary>
public sealed class UpdateAuditableEntitiesInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var dbContext = eventData.Context ?? throw new NullReferenceException();

        var audibleEntities = dbContext.ChangeTracker.Entries<IAuditableEntity>();

        foreach (var entityEntry in audibleEntities)
        {
            if (entityEntry.State == EntityState.Added)
            {
                if (entityEntry.Entity.CreatedAt == default)
                {
                    throw new InvalidOperationException("CreatedAt must be set before saving");
                }
            }

            if (entityEntry.State == EntityState.Modified)
            {
                entityEntry.Entity.UpdateModifiedAt(DateTime.UtcNow);
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}