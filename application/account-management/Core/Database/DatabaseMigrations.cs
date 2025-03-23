using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PlatformPlatform.AccountManagement.Database;

[DbContext(typeof(AccountManagementDbContext))]
[Migration("20250217000000_Initial")]
public sealed class DatabaseMigrations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "EmailConfirmations",
            table => new
            {
                Id = table.Column<string>("varchar(32)", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: false),
                ModifiedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: true),
                Type = table.Column<string>("varchar(20)", nullable: false),
                Email = table.Column<string>("nvarchar(100)", nullable: false),
                OneTimePasswordHash = table.Column<string>("char(84)", nullable: false),
                ValidUntil = table.Column<DateTimeOffset>("datetimeoffset", nullable: false),
                RetryCount = table.Column<int>("int", nullable: false),
                ResendCount = table.Column<int>("int", nullable: false),
                Completed = table.Column<bool>("bit", nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_EmailConfirmations", x => x.Id); }
        );

        migrationBuilder.CreateIndex("IX_EmailConfirmations_Email", "EmailConfirmations", "Email");

        migrationBuilder.CreateTable(
            "Tenants",
            table => new
            {
                Id = table.Column<string>("bigint", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: false),
                ModifiedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: true),
                Name = table.Column<string>("nvarchar(30)", nullable: false),
                State = table.Column<string>("varchar(20)", nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_Tenants", x => x.Id); }
        );

        migrationBuilder.CreateTable(
            "Users",
            table => new
            {
                TenantId = table.Column<long>("bigint", nullable: false),
                Id = table.Column<string>("varchar(32)", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: false),
                ModifiedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: true),
                Email = table.Column<string>("nvarchar(100)", nullable: false),
                FirstName = table.Column<string>("nvarchar(30)", nullable: true),
                LastName = table.Column<string>("nvarchar(30)", nullable: true),
                Title = table.Column<string>("nvarchar(50)", nullable: true),
                Role = table.Column<string>("varchar(20)", nullable: false),
                EmailConfirmed = table.Column<bool>("bit", nullable: false),
                Avatar = table.Column<string>("varchar(150)", nullable: false),
                Locale = table.Column<string>("varchar(5)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
                table.ForeignKey("FK_Users_Tenants_TenantId", x => x.TenantId, "Tenants", "Id");
            }
        );

        migrationBuilder.CreateIndex("IX_Users_TenantId", "Users", "TenantId");

        migrationBuilder.CreateTable(
            "Logins",
            table => new
            {
                TenantId = table.Column<long>("bigint", nullable: false),
                UserId = table.Column<string>("varchar(32)", nullable: false),
                Id = table.Column<string>("varchar(32)", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: false),
                ModifiedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: true),
                EmailConfirmationId = table.Column<string>("varchar(32)", nullable: false),
                Completed = table.Column<bool>("bit", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Logins", x => x.Id);
                table.ForeignKey("FK_Logins_Tenants_TenantId", x => x.TenantId, "Tenants", "Id");
                table.ForeignKey("FK_Logins_User_UserId", x => x.UserId, "Users", "Id");
            }
        );

        migrationBuilder.CreateIndex("IX_Logins_TenantId", "Logins", "TenantId");
        migrationBuilder.CreateIndex("IX_Logins_UserId", "Logins", "UserId");
    }
}
