using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PlatformPlatform.Account.Database.Migrations;

[DbContext(typeof(AccountDbContext))]
[Migration("20260211120000_AddDisputeAndRefundTracking")]
public sealed class AddDisputeAndRefundTracking : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>("DisputedAt", "Subscriptions", "datetimeoffset", nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>("RefundedAt", "Subscriptions", "datetimeoffset", nullable: true);
    }
}
