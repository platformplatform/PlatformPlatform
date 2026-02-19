using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PlatformPlatform.Account.Database.Migrations;

[DbContext(typeof(AccountDbContext))]
[Migration("20260211101300_AddSubscriptionsAndStripeEvents")]
public sealed class AddSubscriptionsAndStripeEvents : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("SuspensionReason", "Tenants", "varchar(30)", nullable: true);
        migrationBuilder.AddColumn<DateTimeOffset>("SuspendedAt", "Tenants", "datetimeoffset", nullable: true);

        migrationBuilder.CreateTable(
            "Subscriptions",
            table => new
            {
                TenantId = table.Column<long>("bigint", nullable: false),
                Id = table.Column<string>("varchar(32)", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: false),
                ModifiedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: true),
                Plan = table.Column<string>("varchar(20)", nullable: false),
                ScheduledPlan = table.Column<string>("varchar(20)", nullable: true),
                StripeCustomerId = table.Column<string>("varchar(32)", nullable: true),
                StripeSubscriptionId = table.Column<string>("varchar(32)", nullable: true),
                CurrentPriceAmount = table.Column<decimal>("decimal(18,2)", nullable: true),
                CurrentPriceCurrency = table.Column<string>("varchar(3)", nullable: true),
                CurrentPeriodEnd = table.Column<DateTimeOffset>("datetimeoffset", nullable: true),
                CancelAtPeriodEnd = table.Column<bool>("bit", nullable: false),
                FirstPaymentFailedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: true),
                CancellationReason = table.Column<string>("varchar(20)", nullable: true),
                CancellationFeedback = table.Column<string>("nvarchar(500)", nullable: true),
                PaymentTransactions = table.Column<string>("nvarchar(max)", nullable: false),
                PaymentMethod = table.Column<string>("nvarchar(max)", nullable: true),
                BillingInfo = table.Column<string>("nvarchar(max)", nullable: true)
            },
            constraints: table => { table.PrimaryKey("PK_Subscriptions", x => x.Id); }
        );

        migrationBuilder.CreateIndex("IX_Subscriptions_TenantId", "Subscriptions", "TenantId", unique: true);

        migrationBuilder.CreateIndex("IX_Subscriptions_StripeCustomerId", "Subscriptions", "StripeCustomerId", unique: true, filter: "StripeCustomerId IS NOT NULL");

        migrationBuilder.CreateTable(
            "StripeEvents",
            table => new
            {
                TenantId = table.Column<long>("bigint", nullable: true),
                Id = table.Column<string>("varchar(32)", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: false),
                ModifiedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: true),
                EventType = table.Column<string>("varchar(50)", nullable: false),
                Status = table.Column<string>("varchar(20)", nullable: false),
                ProcessedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: true),
                StripeCustomerId = table.Column<string>("varchar(32)", nullable: true),
                StripeSubscriptionId = table.Column<string>("varchar(32)", nullable: true),
                Payload = table.Column<string>("nvarchar(max)", nullable: true),
                Error = table.Column<string>("nvarchar(500)", nullable: true)
            },
            constraints: table => { table.PrimaryKey("PK_StripeEvents", x => x.Id); }
        );

        migrationBuilder.CreateIndex("IX_StripeEvents_TenantId", "StripeEvents", "TenantId");
        migrationBuilder.CreateIndex("IX_StripeEvents_StripeCustomerId_Status", "StripeEvents", ["StripeCustomerId", "Status"]);
    }
}
