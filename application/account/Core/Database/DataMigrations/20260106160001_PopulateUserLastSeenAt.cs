using Microsoft.EntityFrameworkCore;
using PlatformPlatform.SharedKernel.Database;

namespace PlatformPlatform.Account.Database.DataMigrations;

public sealed class PopulateUserLastSeenAt(AccountDbContext dbContext) : IDataMigration
{
    public string Id => "20260106160001_PopulateUserLastSeenAt";

    public async Task<string> ExecuteAsync(CancellationToken cancellationToken)
    {
        var rowsAffected = await dbContext.Database.ExecuteSqlRawAsync(
            """
            UPDATE Users
            SET LastSeenAt = COALESCE(ModifiedAt, CreatedAt)
            WHERE EmailConfirmed = 1 AND LastSeenAt IS NULL
            """,
            cancellationToken
        );

        await dbContext.SaveChangesAsync(cancellationToken);

        return $"Updated {rowsAffected} users with LastSeenAt";
    }
}
