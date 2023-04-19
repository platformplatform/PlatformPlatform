using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PlatformPlatform.AccountManagement.Domain.Primitives;

namespace PlatformPlatform.AccountManagement.Infrastructure.Common;

/// <summary>
///     This SaveChangesInterceptor intercept the saving of changes to the database
///     It is designed to automatically update the CreatedAt and ModifiedAt properties of entities implementing
///     the IAuditableEntity interface before they are persisted to the database.
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
                entityEntry.Property(a => a.CreatedAt).CurrentValue = DateTime.UtcNow;

            if (entityEntry.State == EntityState.Modified)
                entityEntry.Property(a => a.ModifiedAt).CurrentValue = DateTime.UtcNow;
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}