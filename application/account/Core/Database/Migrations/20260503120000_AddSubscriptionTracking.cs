using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Account.Database.Migrations;

[DbContext(typeof(AccountDbContext))]
[Migration("20260503120000_AddSubscriptionTracking")]
public sealed class AddSubscriptionTracking : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>("subscribed_since", "subscriptions", "timestamptz", nullable: true);
        migrationBuilder.AddColumn<decimal>("scheduled_price_amount", "subscriptions", "numeric(18,2)", nullable: true);
    }
}
