using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PlatformPlatform.Account.Database.Migrations;

[DbContext(typeof(AccountDbContext))]
[Migration("20260211101302_AddStripeWebhookEvents")]
public sealed class AddStripeWebhookEvents : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "StripeWebhookEvents",
            table => new
            {
                Id = table.Column<string>("varchar(32)", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: false),
                ModifiedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: true),
                StripeEventId = table.Column<string>("varchar(255)", nullable: false),
                EventType = table.Column<string>("varchar(100)", nullable: false),
                ProcessedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_StripeWebhookEvents", x => x.Id); }
        );

        migrationBuilder.CreateIndex("IX_StripeWebhookEvents_StripeEventId", "StripeWebhookEvents", "StripeEventId", unique: true);
    }
}
