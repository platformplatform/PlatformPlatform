using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PlatformPlatform.SharedKernel.Domain;

namespace PlatformPlatform.SharedKernel.EntityFramework;

/// <summary>
///     The SoftDeleteInterceptor intercepts delete operations and converts them to soft deletes
///     for entities implementing <see cref="ISoftDeletable" />. When an entity is marked for deletion,
///     instead of physically removing it from the database, the DeletedAt timestamp is set.
/// </summary>
public sealed class SoftDeleteInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ProcessSoftDeletes(eventData);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        ProcessSoftDeletes(eventData);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void ProcessSoftDeletes(DbContextEventData eventData)
    {
        var dbContext = eventData.Context ?? throw new UnreachableException("The 'eventData.Context' property is unexpectedly null.");

        var timeProvider = dbContext is ITimeProviderSource timeProviderSource
            ? timeProviderSource.TimeProvider
            : TimeProvider.System;

        var deletedEntities = dbContext.ChangeTracker.Entries<ISoftDeletable>()
            .Where(e => e.State is EntityState.Deleted && !e.Entity.ForceHardDelete);

        foreach (var entityEntry in deletedEntities)
        {
            entityEntry.State = EntityState.Modified;
            entityEntry.Entity.MarkAsDeleted(timeProvider.GetUtcNow());
        }
    }
}
