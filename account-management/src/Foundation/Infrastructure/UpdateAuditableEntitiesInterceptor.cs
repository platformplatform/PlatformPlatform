using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PlatformPlatform.Foundation.DddCore;

namespace PlatformPlatform.Foundation.Infrastructure;

/// <summary>
///     The UpdateAuditableEntitiesInterceptor is a SaveChangesInterceptor that updates the ModifiedAt property
///     for IAuditableEntity instances when changes are made to the database.
/// </summary>
public sealed class UpdateAuditableEntitiesInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateEntities(eventData);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void UpdateEntities(DbContextEventData eventData)
    {
        var dbContext = eventData.Context ?? throw new NullReferenceException();

        var audibleEntities = dbContext.ChangeTracker.Entries<IAuditableEntity>();

        foreach (var entityEntry in audibleEntities)
        {
            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (entityEntry.State)
            {
                case EntityState.Added when entityEntry.Entity.CreatedAt == default:
                    throw new InvalidOperationException("CreatedAt must be set before saving.");
                case EntityState.Modified:
                    entityEntry.Entity.UpdateModifiedAt(DateTime.UtcNow);
                    break;
            }
        }
    }
}