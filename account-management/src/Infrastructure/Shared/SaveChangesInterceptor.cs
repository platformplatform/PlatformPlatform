using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using PlatformPlatform.AccountManagement.Domain.Shared;

namespace PlatformPlatform.AccountManagement.Infrastructure.Shared;

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
            switch (entityEntry.State)
            {
                case EntityState.Added when entityEntry.Entity.CreatedAt == default:
                    throw new InvalidOperationException("CreatedAt must be set before saving.");
                case EntityState.Modified:
                    entityEntry.Entity.UpdateModifiedAt(DateTime.UtcNow);
                    break;
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}