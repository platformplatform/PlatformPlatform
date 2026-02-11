using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PlatformPlatform.Account.Database.Migrations;

[DbContext(typeof(AccountDbContext))]
[Migration("20260211101300_AddSubscriptions")]
public sealed class AddSubscriptions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
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
                StripeCustomerId = table.Column<string>("varchar(255)", nullable: true),
                StripeSubscriptionId = table.Column<string>("varchar(255)", nullable: true),
                CurrentPeriodEnd = table.Column<DateTimeOffset>("datetimeoffset", nullable: true),
                CancelAtPeriodEnd = table.Column<bool>("bit", nullable: false),
                FirstPaymentFailedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: true),
                LastNotificationSentAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: true),
                CancellationReason = table.Column<string>("varchar(20)", nullable: true),
                CancellationFeedback = table.Column<string>("nvarchar(500)", nullable: true),
                PaymentTransactions = table.Column<string>("nvarchar(max)", nullable: false),
                PaymentMethod = table.Column<string>("nvarchar(max)", nullable: true)
            },
            constraints: table => { table.PrimaryKey("PK_Subscriptions", x => x.Id); }
        );

        migrationBuilder.CreateIndex("IX_Subscriptions_TenantId", "Subscriptions", "TenantId");
    }
}
