using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Account.Database.Migrations;

[DbContext(typeof(AccountDbContext))]
[Migration("20260508021500_AddBillingEventsAndDriftDetection")]
public sealed class AddBillingEventsAndDriftDetection : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>("subscribed_since", "subscriptions", "timestamptz", nullable: true);
        migrationBuilder.AddColumn<decimal>("scheduled_price_amount", "subscriptions", "numeric(18,2)", nullable: true);
        migrationBuilder.AddColumn<bool>("has_drift_detected", "subscriptions", "boolean", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<DateTimeOffset>("drift_checked_at", "subscriptions", "timestamptz", nullable: true);
        migrationBuilder.AddColumn<string>("drift_discrepancies", "subscriptions", "jsonb", nullable: false, defaultValue: "[]");

        migrationBuilder.CreateIndex("ix_subscriptions_has_drift_detected", "subscriptions", "has_drift_detected", filter: "has_drift_detected = true");

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

        migrationBuilder.AddCheckConstraint(
            "chk_subscriptions_payment_transactions_tax_breakdown",
            "subscriptions",
            """NOT jsonb_path_exists(payment_transactions, '$[*] ? (!(@.AmountExcludingTax.type() == "number") || !(@.TaxAmount.type() == "number"))')"""
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
                event_type = table.Column<string>("text", nullable: false),
                from_plan = table.Column<string>("text", nullable: true),
                to_plan = table.Column<string>("text", nullable: true),
                previous_amount = table.Column<decimal>("numeric(18,2)", nullable: true),
                new_amount = table.Column<decimal>("numeric(18,2)", nullable: true),
                amount_delta = table.Column<decimal>("numeric(18,2)", nullable: true),
                currency = table.Column<string>("text", nullable: true),
                days_on_previous_plan = table.Column<int>("integer", nullable: true),
                days_until_effective = table.Column<int>("integer", nullable: true),
                days_since_cancelled = table.Column<int>("integer", nullable: true),
                scheduled_for = table.Column<DateTimeOffset>("timestamptz", nullable: true),
                effective_at = table.Column<DateTimeOffset>("timestamptz", nullable: true),
                occurred_at = table.Column<DateTimeOffset>("timestamptz", nullable: false),
                cancellation_reason = table.Column<string>("text", nullable: true),
                suspension_reason = table.Column<string>("text", nullable: true),
                stripe_reference = table.Column<string>("text", nullable: false)
            },
            constraints: table => { table.PrimaryKey("pk_billing_events", x => x.id); }
        );

        migrationBuilder.CreateIndex("ix_billing_events_tenant_id_occurred_at", "billing_events", ["tenant_id", "occurred_at"], descending: [false, true]);
        migrationBuilder.CreateIndex("ix_billing_events_occurred_at", "billing_events", "occurred_at", descending: [true]);
        migrationBuilder.CreateIndex("ix_billing_events_subscription_id", "billing_events", "subscription_id");
    }
}
