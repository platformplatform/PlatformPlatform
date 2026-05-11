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

        migrationBuilder.AddCheckConstraint(
            "chk_subscriptions_payment_transactions_amounts_non_negative",
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

        // The platform's architectural promise is that every active Stripe price uses the same currency,
        // derived from Stripe at startup (see PlatformCurrencyStartupResolver). The dashboard MRR handlers
        // sum decimal amounts across every subscription / billing event without grouping by currency, so
        // any row that does not use the platform currency corrupts the totals. The boundary guards in
        // StripeClient and ReconcileTenantWithStripeHandler reject mismatched currencies before they reach
        // persistence; these CHECK constraints are the structural format backstop so the invariant holds
        // even if a future code path forgets the boundary check. The application uppercases currency on
        // read via ToUpperInvariant() before persistence, so the constraint requires the canonical
        // uppercase ISO-4217 form. Basis-only tenants have no current_price_currency (NULL), so the
        // subscriptions constraint allows NULL.
        migrationBuilder.AddCheckConstraint(
            "chk_billing_events_currency_format",
            "billing_events",
            "currency ~ '^[A-Z]{3}$'"
        );

        migrationBuilder.AddCheckConstraint(
            "chk_subscriptions_current_price_currency_format",
            "subscriptions",
            "current_price_currency IS NULL OR current_price_currency ~ '^[A-Z]{3}$'"
        );
    }
}
