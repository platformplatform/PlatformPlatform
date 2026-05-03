using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Account.Database.Migrations;

[DbContext(typeof(AccountDbContext))]
[Migration("20260503130000_BackfillSubscribedSince")]
public sealed class BackfillSubscribedSince : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Subscriptions created before AddSubscriptionTracking have no subscribed_since because the
        // column did not exist when the Basis -> paid transition occurred. Best available proxy for
        // the start of their paid run is the subscription row's created_at timestamp. Only backfill
        // active paid subscriptions (have a Stripe subscription id and are not on the free Basis plan).
        migrationBuilder.Sql(
            """
            UPDATE subscriptions
            SET subscribed_since = created_at
            WHERE subscribed_since IS NULL
              AND stripe_subscription_id IS NOT NULL
              AND plan <> 'Basis';
            """
        );
    }
}
