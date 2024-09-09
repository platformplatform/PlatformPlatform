using System.Net.Sockets;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace PlatformPlatform.SharedKernel.Database;

public class DatabaseMigrationService<T>(T dbContext, ILogger<DatabaseMigrationService<T>> logger)
    where T : DbContext
{
    public void ApplyMigrations()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
        logger.LogInformation("Applying database migrations. Version: {Version}", version);

        const int maxRetryCount = 30;
        for (var retry = 1; retry <= maxRetryCount; retry++)
        {
            try
            {
                if (retry % 5 == 0) logger.LogInformation("Waiting for databases to be ready...");

                var executionStrategy = dbContext.Database.CreateExecutionStrategy();

                executionStrategy.Execute(() => dbContext.Database.Migrate());

                logger.LogInformation("Finished migrating database.");

                return;
            }
            catch (SqlException ex) when (ex.Message.Contains("an error occurred during the pre-login handshake"))
            {
                // Known error in Aspire, when SQL Server is not ready
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
            catch (SocketException ex) when (ex.Message.Contains("Invalid argument"))
            {
                // Known error in Aspire, when SQL Server is not ready
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while applying database migrations.");
                return;
            }
        }

        logger.LogError(" Migration failed after {MaxRetryCount} retries.", maxRetryCount);
    }
}
