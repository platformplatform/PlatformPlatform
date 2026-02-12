using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PlatformPlatform.Account.Database.Migrations;

[DbContext(typeof(AccountDbContext))]
[Migration("20260212140000_AddTrackingFieldsToStripeWebhookEvents")]
public sealed class AddTrackingFieldsToStripeWebhookEvents : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("StripeCustomerId", "StripeWebhookEvents", "varchar(255)", nullable: true);
        migrationBuilder.AddColumn<string>("StripeSubscriptionId", "StripeWebhookEvents", "varchar(255)", nullable: true);
        migrationBuilder.AddColumn<long>("TenantId", "StripeWebhookEvents", "bigint", nullable: true);
        migrationBuilder.AddColumn<string>("Payload", "StripeWebhookEvents", "nvarchar(max)", nullable: true);

        migrationBuilder.CreateIndex("IX_StripeWebhookEvents_StripeCustomerId", "StripeWebhookEvents", "StripeCustomerId");
        migrationBuilder.CreateIndex("IX_StripeWebhookEvents_TenantId", "StripeWebhookEvents", "TenantId");
    }
}
