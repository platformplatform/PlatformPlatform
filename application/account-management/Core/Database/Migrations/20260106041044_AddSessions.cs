using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PlatformPlatform.AccountManagement.Database;

namespace PlatformPlatform.AccountManagement.Database.Migrations;

[DbContext(typeof(AccountManagementDbContext))]
[Migration("20260106041044_AddSessions")]
public sealed class AddSessions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "Sessions",
            table => new
            {
                TenantId = table.Column<long>("bigint", nullable: false),
                Id = table.Column<string>("varchar(32)", nullable: false),
                UserId = table.Column<string>("varchar(32)", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: false),
                ModifiedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: true),
                RefreshTokenJti = table.Column<string>("varchar(32)", nullable: false),
                PreviousRefreshTokenJti = table.Column<string>("varchar(32)", nullable: true),
                RefreshTokenVersion = table.Column<int>("int", nullable: false),
                DeviceType = table.Column<string>("varchar(20)", nullable: false),
                UserAgent = table.Column<string>("nvarchar(500)", nullable: false),
                IpAddress = table.Column<string>("varchar(45)", nullable: false),
                RevokedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: true),
                RevokedReason = table.Column<string>("varchar(20)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Sessions", x => x.Id);
                table.ForeignKey("FK_Sessions_Tenants_TenantId", x => x.TenantId, "Tenants", "Id");
                table.ForeignKey("FK_Sessions_Users_UserId", x => x.UserId, "Users", "Id", onDelete: ReferentialAction.Cascade);
            }
        );

        migrationBuilder.CreateIndex("IX_Sessions_TenantId", "Sessions", "TenantId");
        migrationBuilder.CreateIndex("IX_Sessions_UserId", "Sessions", "UserId");
    }
}
