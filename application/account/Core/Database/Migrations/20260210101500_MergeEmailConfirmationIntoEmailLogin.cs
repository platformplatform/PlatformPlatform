using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PlatformPlatform.Account.Database.Migrations;

[DbContext(typeof(AccountDbContext))]
[Migration("20260210101500_MergeEmailConfirmationIntoEmailLogin")]
public sealed class MergeEmailConfirmationIntoEmailLogin : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("Logins");

        migrationBuilder.DropColumn("ValidUntil", "EmailConfirmations");

        migrationBuilder.DropIndex("IX_EmailConfirmations_Email", "EmailConfirmations");

        migrationBuilder.RenameTable("EmailConfirmations", newName: "EmailLogins");

        migrationBuilder.CreateIndex("IX_EmailLogins_Email", "EmailLogins", "Email");

        migrationBuilder.Sql("UPDATE EmailLogins SET Id = REPLACE(Id, 'econf_', 'emlog_') WHERE Id LIKE 'econf_%'");
    }
}
