using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace PlatformPlatform.Foundation.InfrastructureCore.EntityFramework;

/// <summary>
///     The EntityValidationSaveChangesInterceptor is a SaveChangesInterceptor that validates all entities before
///     changes are saved to the database.
/// </summary>
public class EntityValidationSaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly IServiceProvider _serviceProvider;

    public EntityValidationSaveChangesInterceptor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        var dbContext = eventData.Context ?? throw new NullReferenceException();
        var entries = dbContext.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified);

        foreach (var entityEntry in entries)
        {
            var entityType = entityEntry.Entity.GetType();
            var validatorType = typeof(IValidator<>).MakeGenericType(entityType);

            if (_serviceProvider.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var context = new ValidationContext<object>(entityEntry.Entity);
            var validationResult = validator.Validate(context);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }
        }

        return result;
    }
}