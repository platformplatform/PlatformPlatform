namespace PlatformPlatform.SharedKernel.Database;

/// <summary>Data migration that runs after schema migrations. Tracked in __DataMigrationsHistory table.</summary>
[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
public interface IDataMigration
{
    /// <summary>Migration ID in format 'YYYYMMDDHHmmss_ClassName'. Must match class name suffix.</summary>
    public string Id { get; }

    /// <summary>Executes migration, returns summary. Must call dbContext.SaveChangesAsync() first.</summary>
    public Task<string> ExecuteAsync(CancellationToken cancellationToken);
}
