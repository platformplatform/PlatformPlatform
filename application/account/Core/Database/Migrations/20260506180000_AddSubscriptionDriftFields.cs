using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Account.Database.Migrations;

[DbContext(typeof(AccountDbContext))]
[Migration("20260506180000_AddSubscriptionDriftFields")]
public sealed class AddSubscriptionDriftFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>("has_drift_detected", "subscriptions", "boolean", nullable: false, defaultValue: false);
        migrationBuilder.AddColumn<DateTimeOffset>("drift_checked_at", "subscriptions", "timestamptz", nullable: true);
        migrationBuilder.AddColumn<string>("drift_discrepancies", "subscriptions", "jsonb", nullable: false, defaultValue: "[]");

        migrationBuilder.CreateIndex("ix_subscriptions_has_drift_detected", "subscriptions", "has_drift_detected", filter: "has_drift_detected = true");
    }
}
