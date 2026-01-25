using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PlatformPlatform.Account.Database.Migrations;

[DbContext(typeof(AccountDbContext))]
[Migration("20260116194522_RenameLoginsToEmailLogins")]
public sealed class RenameLoginsToEmailLogins : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_Logins_User_UserId", table: "Logins");

        migrationBuilder.RenameTable(name: "Logins", newName: "EmailLogins");

        migrationBuilder.Sql("UPDATE EmailLogins SET Id = REPLACE(Id, 'login_', 'emlog_') WHERE Id LIKE 'login_%'");

        migrationBuilder.AddForeignKey(
            name: "FK_EmailLogins_Users_UserId",
            table: "EmailLogins",
            column: "UserId",
            principalTable: "Users",
            principalColumn: "Id",
            onDelete: ReferentialAction.Cascade);
    }
}
