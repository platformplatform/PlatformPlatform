using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Account.Database.Migrations;

[DbContext(typeof(AccountDbContext))]
[Migration("20260520001657_AddSupportTickets")]
public sealed class AddSupportTickets : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "support_tickets",
            table => new
            {
                tenant_id = table.Column<long>("bigint", nullable: false),
                id = table.Column<string>("text", nullable: false),
                created_at = table.Column<DateTimeOffset>("timestamptz", nullable: false),
                modified_at = table.Column<DateTimeOffset>("timestamptz", nullable: true),
                reporter_id = table.Column<string>("text", nullable: false),
                reporter_role_snapshot = table.Column<string>("text", nullable: false),
                reporter_email_snapshot = table.Column<string>("text", nullable: false),
                subject = table.Column<string>("text", nullable: false),
                category = table.Column<string>("text", nullable: false),
                status = table.Column<string>("text", nullable: false),
                assignee = table.Column<string>("jsonb", nullable: true),
                last_activity_at = table.Column<DateTimeOffset>("timestamptz", nullable: false),
                resolved_at = table.Column<DateTimeOffset>("timestamptz", nullable: true),
                closed_at = table.Column<DateTimeOffset>("timestamptz", nullable: true),
                csat = table.Column<string>("jsonb", nullable: true),
                messages = table.Column<string>("jsonb", nullable: false, defaultValue: "[]"),
                history_events = table.Column<string>("jsonb", nullable: false, defaultValue: "[]")
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_support_tickets", x => x.id);
                table.ForeignKey("fk_support_tickets_tenants_tenant_id", x => x.tenant_id, "tenants", "id");
            }
        );

        migrationBuilder.CreateIndex("ix_support_tickets_tenant_id", "support_tickets", "tenant_id");
        migrationBuilder.CreateIndex("ix_support_tickets_tenant_id_reporter_id", "support_tickets", ["tenant_id", "reporter_id"]);
        migrationBuilder.CreateIndex("ix_support_tickets_status", "support_tickets", "status");
    }
}
