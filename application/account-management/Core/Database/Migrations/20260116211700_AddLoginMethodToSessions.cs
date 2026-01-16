using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PlatformPlatform.AccountManagement.Database.Migrations;

[DbContext(typeof(AccountManagementDbContext))]
[Migration("20260116211700_AddLoginMethodToSessions")]
public sealed class AddLoginMethodToSessions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("LoginMethod", "Sessions", "varchar(20)", nullable: false, defaultValue: "OneTimePassword");
        migrationBuilder.AlterColumn<string>("LoginMethod", "Sessions", "varchar(20)", nullable: false);
    }
}
