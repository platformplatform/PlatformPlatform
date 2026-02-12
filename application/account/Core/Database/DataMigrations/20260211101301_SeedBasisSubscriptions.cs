using Microsoft.EntityFrameworkCore;
using PlatformPlatform.Account.Features.Subscriptions.Domain;
using PlatformPlatform.SharedKernel.Database;

namespace PlatformPlatform.Account.Database.DataMigrations;

public sealed class SeedBasisSubscriptions(AccountDbContext dbContext) : IDataMigration
{
    public string Id => "20260124012001_SeedBasisSubscriptions";

    public async Task<string> ExecuteAsync(CancellationToken cancellationToken)
    {
        var tenantIds = await dbContext.Database
            .SqlQueryRaw<long>(
                """
                SELECT t.Id AS Value
                FROM Tenants t
                WHERE NOT EXISTS (SELECT 1 FROM Subscriptions s WHERE s.TenantId = t.Id)
                """
            )
            .ToListAsync(cancellationToken);

        foreach (var tenantId in tenantIds)
        {
            var subscriptionId = SubscriptionId.NewId();
            await dbContext.Database.ExecuteSqlAsync(
                $"""
                 INSERT INTO Subscriptions (TenantId, Id, CreatedAt, ModifiedAt, [Plan], ScheduledPlan, StripeCustomerId, StripeSubscriptionId, CurrentPeriodEnd, CancelAtPeriodEnd, FirstPaymentFailedAt, LastNotificationSentAt, PaymentTransactions, PaymentMethod, BillingInfo)
                 VALUES ({tenantId}, {subscriptionId.Value}, GETUTCDATE(), NULL, 'Basis', NULL, NULL, NULL, NULL, 0, NULL, NULL, '[]', NULL, NULL)
                 """,
                cancellationToken
            );
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return $"Created {tenantIds.Count} basis subscriptions for existing tenants";
    }
}
