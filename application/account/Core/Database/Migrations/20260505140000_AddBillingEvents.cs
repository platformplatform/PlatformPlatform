using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Account.Database.Migrations;

[DbContext(typeof(AccountDbContext))]
[Migration("20260505140000_AddBillingEvents")]
public sealed class AddBillingEvents : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
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
