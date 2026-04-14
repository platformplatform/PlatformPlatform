using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Account.Database.Migrations;

[DbContext(typeof(AccountDbContext))]
[Migration("20260414120000_AddTeams")]
public sealed class AddTeams : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "teams",
            table => new
            {
                tenant_id = table.Column<long>("bigint", nullable: false),
                id = table.Column<string>("text", nullable: false),
                created_at = table.Column<DateTimeOffset>("timestamptz", nullable: false),
                modified_at = table.Column<DateTimeOffset>("timestamptz", nullable: true),
                name = table.Column<string>("text", nullable: false),
                description = table.Column<string>("text", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_teams", x => x.id);
                table.ForeignKey("fk_teams_tenants_tenant_id", x => x.tenant_id, "tenants", "id");
            }
        );

        migrationBuilder.CreateIndex("ix_teams_tenant_id", "teams", "tenant_id");
        migrationBuilder.CreateIndex("ix_teams_tenant_id_name", "teams", ["tenant_id", "name"], unique: true);
    }
}
