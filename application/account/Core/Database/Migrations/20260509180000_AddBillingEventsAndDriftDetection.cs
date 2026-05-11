using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Account.Database.Migrations;

[DbContext(typeof(AccountDbContext))]
[Migration("20260509180000_AddBillingEventsAndDriftDetection")]
public sealed class AddBillingEventsAndDriftDetection : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>("subscribed_since", "subscriptions", "timestamptz", nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>("last_synced_stripe_event_created_at", "subscriptions", "timestamptz", nullable: true);
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
            """NOT jsonb_path_exists(payment_transactions, '$[*] ? (!(@.AmountExcludingTax.type() == "number") || !(@.TaxAmount.type() == "number") || @.AmountExcludingTax < 0 || @.TaxAmount < 0)')"""
        );

        // The billing_events table is append-only. The unique index on stripe_event_id enforces strict
        // 1:1 with Stripe events: every recognized Stripe event yields exactly one row. Stripe's events.list
        // API has a 30-day retention window (see https://docs.stripe.com/api/events), so the local
        // stripe_events table is the authoritative source for replays beyond that window.
        // Hard rule: NO migration ever drops, deletes from, or truncates this table. Schema changes use
        // ALTER TABLE ADD/DROP COLUMN. Forensics and audit depend on full history being preserved.
        // tenant_id is the soft-scope query filter for ITenantScopedEntity; no FK to tenants because the
        // back-office is cross-tenant by design and uses IgnoreQueryFilters([QueryFilterNames.Tenant]).
        // modified_at is inherited from the framework's AggregateRoot shape and remains NULL by design —
        // billing_events is append-only forever (rows are never updated after insert).
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

        // stripe_events extensions for the multi-source reconciliation architecture:
        // - api_version: pinned at event creation per https://docs.stripe.com/api/events; lets the
        //   replayer dispatch to the correct payload resolver when Stripe ships a new API version.
        // - payload_hash: SHA-256 of the raw payload at first observation; lets AcknowledgeStripeWebhook
        //   detect StripeEventPayloadDivergence (same id, different payload) without comparing JSON bodies.
        // - recovered_at / recovery_source: non-null when the event was added by reconciliation
        //   (events.list or webhook_endpoint_deliveries) rather than via webhook delivery — forensic
        //   marker that a webhook delivery was missed.
        migrationBuilder.AddColumn<string>("api_version", "stripe_events", "text", nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>("recovered_at", "stripe_events", "timestamptz", nullable: true);
        migrationBuilder.AddColumn<string>("recovery_source", "stripe_events", "text", nullable: true);
        migrationBuilder.AddColumn<string>("payload_hash", "stripe_events", "text", nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>("stripe_created_at", "stripe_events", "timestamptz", nullable: true);

        migrationBuilder.CreateIndex("ix_stripe_events_recovered_at", "stripe_events", "recovered_at", filter: "recovered_at IS NOT NULL");

        // Backfill stripe_created_at from the archived payload's "created" field (Stripe event epoch
        // seconds — see https://docs.stripe.com/api/events). The replayer orders events and writes
        // BillingEvent.OccurredAt from StripeCreatedAt ?? CreatedAt so legacy rows recorded before this
        // column existed fall back to ingestion time; this backfill upgrades them to Stripe's authoritative
        // event time wherever the payload was preserved.
        migrationBuilder.Sql(
            """
            UPDATE stripe_events
            SET stripe_created_at = to_timestamp((payload::jsonb ->> 'created')::numeric)
            WHERE stripe_created_at IS NULL
              AND payload IS NOT NULL
              AND payload::jsonb ->> 'created' IS NOT NULL;
            """
        );

        // v1 stance: only DKK is supported. The dashboard MRR handlers sum decimal amounts across every
        // subscription / billing event without grouping by currency, so any non-DKK row corrupts the totals.
        // The boundary guard in ReconcileTenantWithStripeHandler rejects non-DKK syncs before they reach
        // persistence; these CHECK constraints are the structural backstop so the invariant holds even if
        // a future code path forgets the boundary check. Basis-only tenants have no current_price_currency
        // (NULL), so the subscriptions constraint allows NULL. billing_events.currency is always populated
        // by the Stripe event payload, so the billing_events constraint requires the literal 'DKK'.
        migrationBuilder.AddCheckConstraint(
            "chk_billing_events_currency_dkk",
            "billing_events",
            "currency = 'DKK'"
        );

        migrationBuilder.AddCheckConstraint(
            "chk_subscriptions_current_price_currency_dkk",
            "subscriptions",
            "current_price_currency IS NULL OR current_price_currency = 'DKK'"
        );
    }
}
