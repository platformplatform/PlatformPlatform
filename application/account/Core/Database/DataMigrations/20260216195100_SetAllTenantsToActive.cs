using Microsoft.EntityFrameworkCore;
using PlatformPlatform.SharedKernel.Database;

namespace PlatformPlatform.Account.Database.DataMigrations;

public sealed class SetAllTenantsToActive(AccountDbContext dbContext) : IDataMigration
{
    public string Id => "20260216195100_SetAllTenantsToActive";

    public async Task<string> ExecuteAsync(CancellationToken cancellationToken)
    {
        var updatedCount = await dbContext.Database.ExecuteSqlAsync(
            $"UPDATE Tenants SET State = 'Active' WHERE State NOT IN ('Active', 'Suspended')",
            cancellationToken
        );

        await dbContext.SaveChangesAsync(cancellationToken);

        return $"Updated {updatedCount} tenants to Active state";
    }
}
