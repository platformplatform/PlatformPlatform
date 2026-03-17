namespace SharedKernel.Database;

/// <summary>Data migration that runs after schema migrations. Tracked in __data_migrations_history table.</summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public interface IDataMigration
{
    /// <summary>Migration ID in format 'YYYYMMDDHHmmss_ClassName'. Must match class name suffix.</summary>
    public string Id { get; }

    /// <summary>Maximum duration for this migration. Must not exceed 20 minutes.</summary>
    public TimeSpan Timeout { get; }

    /// <summary>
    ///     When true, the runner skips the transaction wrapper and the migration manages its own commits. Use for large
    ///     batch operations that need chunked processing.
    /// </summary>
    public bool ManagesOwnTransactions => false;

    /// <summary>Executes migration, returns summary. Must call dbContext.SaveChangesAsync() first.</summary>
    public Task<string> ExecuteAsync(CancellationToken cancellationToken);
}
