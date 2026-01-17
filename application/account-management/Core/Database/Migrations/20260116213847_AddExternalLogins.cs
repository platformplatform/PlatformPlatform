using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PlatformPlatform.AccountManagement.Database.Migrations;

[DbContext(typeof(AccountManagementDbContext))]
[Migration("20260116213847_AddExternalLogins")]
public sealed class AddExternalLogins : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "ExternalLogins",
            table => new
            {
                Id = table.Column<string>("varchar(32)", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: false),
                ModifiedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: true),
                ProviderType = table.Column<string>("varchar(20)", nullable: false),
                FlowType = table.Column<string>("varchar(20)", nullable: false),
                StateToken = table.Column<string>("varchar(512)", nullable: false),
                CodeVerifier = table.Column<string>("char(128)", nullable: false),
                BrowserFingerprint = table.Column<string>("char(64)", nullable: false),
                ReturnPath = table.Column<string>("varchar(200)", nullable: true),
                Locale = table.Column<string>("varchar(10)", nullable: true),
                LoginResult = table.Column<string>("varchar(30)", nullable: true)
            },
            constraints: table => { table.PrimaryKey("PK_ExternalLogins", x => x.Id); }
        );

        migrationBuilder.AddColumn<string>("ExternalIdentities", "Users", "nvarchar(max)", nullable: false, defaultValue: "[]");
    }
}
