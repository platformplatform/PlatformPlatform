using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SharedKernel.Domain;
using SharedKernel.PipelineBehaviors;

namespace SharedKernel.Persistence;

/// <summary>
///     UnitOfWork is an implementation of the IUnitOfWork interface from the Domain layer. It is responsible for
///     committing any changes to the application specific DbContext and saving them to the database. The UnitOfWork is
///     called from the <see cref="UnitOfWorkPipelineBehavior{TRequest,TResponse}" /> in the Application layer.
/// </summary>
public sealed class UnitOfWork(DbContext dbContext) : IUnitOfWork
{
    private const string PostgresProviderName = "Npgsql.EntityFrameworkCore.PostgreSQL";

    public async Task<string?> CommitAsync(CancellationToken cancellationToken)
    {
        if (dbContext.ChangeTracker.Entries<IAggregateRoot>().Any(e => e.Entity.DomainEvents.Count != 0))
        {
            throw new InvalidOperationException("Domain events must be handled before committing the UnitOfWork.");
        }

        if (dbContext.Database.ProviderName != PostgresProviderName)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return null;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var connection = dbContext.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT pg_current_xact_id()::text";
        command.Transaction = dbContext.Database.CurrentTransaction!.GetDbTransaction();
        var txid = (string?)await command.ExecuteScalarAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return txid;
    }
}
