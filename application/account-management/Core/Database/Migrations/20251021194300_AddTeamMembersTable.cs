using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PlatformPlatform.AccountManagement.Database.Migrations;

[DbContext(typeof(AccountManagementDbContext))]
[Migration("20251021194300_AddTeamMembersTable")]
public sealed class AddTeamMembersTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "TeamMembers",
            table => new
            {
                TenantId = table.Column<long>("bigint", nullable: false),
                Id = table.Column<string>("varchar(32)", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: false),
                ModifiedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: true),
                TeamId = table.Column<string>("varchar(32)", nullable: false),
                UserId = table.Column<string>("varchar(32)", nullable: false),
                Role = table.Column<int>("int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TeamMembers", x => x.Id);
                table.ForeignKey("FK_TeamMembers_Tenants_TenantId", x => x.TenantId, "Tenants", "Id");
                table.ForeignKey("FK_TeamMembers_Teams_TeamId", x => x.TeamId, "Teams", "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey("FK_TeamMembers_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Restrict);
            }
        );

        migrationBuilder.CreateIndex("IX_TeamMembers_TeamId_UserId", "TeamMembers", new[] { "TeamId", "UserId" }, unique: true);
        migrationBuilder.CreateIndex("IX_TeamMembers_UserId", "TeamMembers", "UserId");
        migrationBuilder.CreateIndex("IX_TeamMembers_TenantId", "TeamMembers", "TenantId");
    }
}
