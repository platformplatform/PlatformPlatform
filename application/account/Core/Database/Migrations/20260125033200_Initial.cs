using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PlatformPlatform.Account.Database.Migrations;

[DbContext(typeof(AccountDbContext))]
[Migration("20260125033200_Initial")]
public sealed class Initial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "Tenants",
            table => new
            {
                Id = table.Column<string>("bigint", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: false),
                ModifiedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: true),
                DeletedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: true),
                Name = table.Column<string>("nvarchar(30)", nullable: false),
                State = table.Column<string>("varchar(20)", nullable: false),
                Logo = table.Column<string>("varchar(150)", nullable: false, defaultValue: "{}")
            },
            constraints: table => { table.PrimaryKey("PK_Tenants", x => x.Id); }
        );

        migrationBuilder.CreateTable(
            "EmailLogins",
            table => new
            {
                Id = table.Column<string>("varchar(32)", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: false),
                ModifiedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: true),
                Type = table.Column<string>("varchar(20)", nullable: false),
                Email = table.Column<string>("nvarchar(100)", nullable: false),
                OneTimePasswordHash = table.Column<string>("char(84)", nullable: false),
                RetryCount = table.Column<int>("int", nullable: false),
                ResendCount = table.Column<int>("int", nullable: false),
                Completed = table.Column<bool>("bit", nullable: false)
            },
            constraints: table => { table.PrimaryKey("PK_EmailLogins", x => x.Id); }
        );

        migrationBuilder.CreateIndex("IX_EmailLogins_Email", "EmailLogins", "Email");

        migrationBuilder.CreateTable(
            "ExternalLogins",
            table => new
            {
                Id = table.Column<string>("varchar(32)", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: false),
                ModifiedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: true),
                Type = table.Column<string>("varchar(20)", nullable: false),
                ProviderType = table.Column<string>("varchar(20)", nullable: false),
                Email = table.Column<string>("varchar(100)", nullable: true),
                CodeVerifier = table.Column<string>("char(128)", nullable: false),
                Nonce = table.Column<string>("char(43)", nullable: false),
                BrowserFingerprint = table.Column<string>("char(64)", nullable: false),
                LoginResult = table.Column<string>("varchar(30)", nullable: true)
            },
            constraints: table => { table.PrimaryKey("PK_ExternalLogins", x => x.Id); }
        );

        migrationBuilder.CreateTable(
            "Users",
            table => new
            {
                TenantId = table.Column<long>("bigint", nullable: false),
                Id = table.Column<string>("varchar(32)", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: false),
                ModifiedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: true),
                DeletedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: true),
                LastSeenAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: true),
                Email = table.Column<string>("nvarchar(100)", nullable: false),
                ExternalIdentities = table.Column<string>("nvarchar(max)", nullable: false, defaultValue: "[]"),
                EmailConfirmed = table.Column<bool>("bit", nullable: false),
                FirstName = table.Column<string>("nvarchar(30)", nullable: true),
                LastName = table.Column<string>("nvarchar(30)", nullable: true),
                Title = table.Column<string>("nvarchar(50)", nullable: true),
                Role = table.Column<string>("varchar(20)", nullable: false),
                Locale = table.Column<string>("varchar(5)", nullable: false),
                Avatar = table.Column<string>("varchar(150)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
                table.ForeignKey("FK_Users_Tenants_TenantId", x => x.TenantId, "Tenants", "Id");
            }
        );

        migrationBuilder.CreateIndex("IX_Users_TenantId", "Users", "TenantId");
        migrationBuilder.CreateIndex("IX_Users_TenantId_Email", "Users", ["TenantId", "Email"], unique: true, filter: "[DeletedAt] IS NULL");

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
                LoginMethod = table.Column<string>("varchar(20)", nullable: false),
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
