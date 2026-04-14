using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Account.Database.Migrations;

[DbContext(typeof(AccountDbContext))]
[Migration("20260414121000_AddTeamMembers")]
public sealed class AddTeamMembers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "team_members",
            table => new
            {
                tenant_id = table.Column<long>("bigint", nullable: false),
                id = table.Column<string>("text", nullable: false),
                team_id = table.Column<string>("text", nullable: false),
                user_id = table.Column<string>("text", nullable: false),
                created_at = table.Column<DateTimeOffset>("timestamptz", nullable: false),
                modified_at = table.Column<DateTimeOffset>("timestamptz", nullable: true),
                role = table.Column<string>("text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_team_members", x => x.id);
                table.ForeignKey("fk_team_members_teams_team_id", x => x.team_id, "teams", "id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("fk_team_members_users_user_id", x => x.user_id, "users", "id", onDelete: ReferentialAction.Cascade);
            }
        );

        migrationBuilder.CreateIndex("ix_team_members_tenant_id", "team_members", "tenant_id");
        migrationBuilder.CreateIndex("ix_team_members_user_id", "team_members", "user_id");
        migrationBuilder.CreateIndex("ix_team_members_team_id_user_id", "team_members", ["team_id", "user_id"], unique: true);
    }
}
