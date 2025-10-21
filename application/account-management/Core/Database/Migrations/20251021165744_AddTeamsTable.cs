using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PlatformPlatform.AccountManagement.Database.Migrations;

[DbContext(typeof(AccountManagementDbContext))]
[Migration("20251021165744_AddTeamsTable")]
public sealed class AddTeamsTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "Teams",
            table => new
            {
                TenantId = table.Column<long>("bigint", nullable: false),
                Id = table.Column<string>("varchar(32)", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: false),
                ModifiedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: true),
                Name = table.Column<string>("nvarchar(100)", nullable: false),
                Description = table.Column<string>("nvarchar(500)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Teams", x => x.Id);
                table.ForeignKey("FK_Teams_Tenants_TenantId", x => x.TenantId, "Tenants", "Id");
            }
        );

        migrationBuilder.CreateIndex("IX_Teams_TenantId", "Teams", "TenantId");
        migrationBuilder.CreateIndex("IX_Teams_TenantId_Name", "Teams", new[] { "TenantId", "Name" }, unique: true);
    }
}
