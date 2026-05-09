using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Account.Database.Migrations;

[DbContext(typeof(AccountDbContext))]
[Migration("20260509120000_AddBillingEventsAndDriftDetection")]
public sealed class AddBillingEventsAndDriftDetection : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Subscription drift columns. IF NOT EXISTS so this migration is idempotent on staging where an
        // earlier iteration of this migration already added them. Removed before merging to main; new
        // environments only see plain ADD COLUMN.
        migrationBuilder.Sql("ALTER TABLE subscriptions ADD COLUMN IF NOT EXISTS subscribed_since timestamptz;");
        migrationBuilder.Sql("ALTER TABLE subscriptions ADD COLUMN IF NOT EXISTS scheduled_price_amount numeric(18,2);");
        migrationBuilder.Sql("ALTER TABLE subscriptions ADD COLUMN IF NOT EXISTS has_drift_detected boolean NOT NULL DEFAULT false;");
        migrationBuilder.Sql("ALTER TABLE subscriptions ADD COLUMN IF NOT EXISTS drift_checked_at timestamptz;");
        migrationBuilder.Sql("ALTER TABLE subscriptions ADD COLUMN IF NOT EXISTS drift_discrepancies jsonb NOT NULL DEFAULT '[]';");

        migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_subscriptions_has_drift_detected ON subscriptions (has_drift_detected) WHERE has_drift_detected = true;");

        // Subscriptions created before this migration have no subscribed_since because the column did not
        // exist when the Basis -> paid transition occurred. Best available proxy for the start of their paid
        // run is the subscription row's created_at timestamp. Only backfill active paid subscriptions
        // (those that have a Stripe subscription id and are not on the free Basis plan).
        migrationBuilder.Sql(
            """
            UPDATE subscriptions
            SET subscribed_since = created_at
            WHERE subscribed_since IS NULL
              AND stripe_subscription_id IS NOT NULL
              AND plan <> 'Basis';
            """
        );

        // PaymentTransaction.AmountExcludingTax and TaxAmount became non-nullable in the C# domain alongside
        // this migration. Existing rows synced from Stripe before that change may have those keys missing or
        // null. Default AmountExcludingTax to the gross Amount and TaxAmount to 0 so the CHECK constraint
        // below passes. The next Stripe sync per tenant overwrites these with the real breakdown.
        migrationBuilder.Sql(
            """
            UPDATE subscriptions
            SET payment_transactions = (
                SELECT jsonb_agg(
                    e || jsonb_build_object(
                        'AmountExcludingTax', COALESCE((e->>'AmountExcludingTax')::numeric, (e->>'Amount')::numeric, 0),
                        'TaxAmount', COALESCE((e->>'TaxAmount')::numeric, 0)
                    )
                )
                FROM jsonb_array_elements(payment_transactions) e
            )
            WHERE jsonb_array_length(payment_transactions) > 0
              AND jsonb_path_exists(payment_transactions, '$[*] ? (!(@.AmountExcludingTax.type() == "number") || !(@.TaxAmount.type() == "number"))');
            """
        );

        // Add the check constraint only if it doesn't exist. Removed before merging to main; new
        // environments only see a plain ALTER TABLE … ADD CONSTRAINT.
        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint
                    WHERE conname = 'chk_subscriptions_payment_transactions_tax_breakdown'
                      AND conrelid = 'subscriptions'::regclass
                ) THEN
                    ALTER TABLE subscriptions
                    ADD CONSTRAINT chk_subscriptions_payment_transactions_tax_breakdown
                    CHECK (NOT jsonb_path_exists(payment_transactions, '$[*] ? (!(@.AmountExcludingTax.type() == "number") || !(@.TaxAmount.type() == "number"))'));
                END IF;
            END $$;
            """
        );

        migrationBuilder.CreateTable(
            "billing_events",
            table => new
            {
                tenant_id = table.Column<long>("bigint", nullable: false),
                id = table.Column<string>("text", nullable: false),
                subscription_id = table.Column<string>("text", nullable: false),
                created_at = table.Column<DateTimeOffset>("timestamptz", nullable: false),
                modified_at = table.Column<DateTimeOffset>("timestamptz", nullable: true),
                stripe_event_id = table.Column<string>("text", nullable: false),
                event_type = table.Column<string>("text", nullable: false),
                from_plan = table.Column<string>("text", nullable: true),
                to_plan = table.Column<string>("text", nullable: true),
                previous_amount = table.Column<decimal>("numeric(18,2)", nullable: true),
                new_amount = table.Column<decimal>("numeric(18,2)", nullable: true),
                amount_delta = table.Column<decimal>("numeric(18,2)", nullable: true),
                committed_mrr = table.Column<decimal>("numeric(18,2)", nullable: false),
                currency = table.Column<string>("text", nullable: true),
                occurred_at = table.Column<DateTimeOffset>("timestamptz", nullable: false),
                cancellation_reason = table.Column<string>("text", nullable: true),
                suspension_reason = table.Column<string>("text", nullable: true)
            },
            constraints: table => { table.PrimaryKey("pk_billing_events", x => x.id); }
        );

        migrationBuilder.CreateIndex("ix_billing_events_stripe_event_id", "billing_events", "stripe_event_id", unique: true);
        migrationBuilder.CreateIndex("ix_billing_events_tenant_id_occurred_at", "billing_events", ["tenant_id", "occurred_at"], descending: [false, true]);
        migrationBuilder.CreateIndex("ix_billing_events_occurred_at", "billing_events", "occurred_at", descending: [true]);
        migrationBuilder.CreateIndex("ix_billing_events_subscription_id", "billing_events", "subscription_id");
    }
}
