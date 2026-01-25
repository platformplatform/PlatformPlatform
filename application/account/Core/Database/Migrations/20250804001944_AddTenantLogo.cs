using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PlatformPlatform.Account.Database.Migrations;

[DbContext(typeof(AccountDbContext))]
[Migration("20250804001944_AddTenantLogo")]
public sealed class AddTenantLogo : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Logo",
            table: "Tenants",
            type: "varchar(150)",
            nullable: false,
            defaultValue: "{}");
    }
}