using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PlatformPlatform.Account.Database.Migrations;

[DbContext(typeof(AccountDbContext))]
[Migration("20260116234946_RemoveEmailConfirmationValidUntil")]
public sealed class RemoveEmailConfirmationValidUntil : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("ValidUntil", "EmailConfirmations");
    }
}
