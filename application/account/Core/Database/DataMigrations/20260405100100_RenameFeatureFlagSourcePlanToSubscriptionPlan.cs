using Microsoft.EntityFrameworkCore;
using SharedKernel.Database;

namespace Account.Database.DataMigrations;

public sealed class RenameFeatureFlagSourcePlanToSubscriptionPlan(AccountDbContext dbContext) : IDataMigration
{
    public string Id => "20260405100100_RenameFeatureFlagSourcePlanToSubscriptionPlan";

    public TimeSpan Timeout { get; } = TimeSpan.FromMinutes(1);

    public async Task<string> ExecuteAsync(CancellationToken cancellationToken)
    {
        var updatedCount = await dbContext.Database.ExecuteSqlRawAsync(
            "UPDATE feature_flags SET source = 'SubscriptionPlan' WHERE source = 'Plan'",
            cancellationToken
        );

        await dbContext.SaveChangesAsync(cancellationToken);

        return $"Updated {updatedCount} feature flag rows from source 'Plan' to 'SubscriptionPlan'";
    }
}
