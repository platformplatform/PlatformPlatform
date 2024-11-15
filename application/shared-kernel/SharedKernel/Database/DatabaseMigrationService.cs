using Microsoft.EntityFrameworkCore;

namespace PlatformPlatform.SharedKernel.Database;

public class DatabaseMigrationService<TContext>(TContext dbContext, ILogger<DatabaseMigrationService<TContext>> logger)
    where TContext : DbContext
{
    public void ApplyMigrations()
    {
        logger.LogInformation("Applying database migrations. Version: {Version}",
            Assembly.GetExecutingAssembly().GetName().Version
        );

        dbContext.Database.CreateExecutionStrategy().Execute(() => { dbContext.Database.Migrate(); });
    }
}
