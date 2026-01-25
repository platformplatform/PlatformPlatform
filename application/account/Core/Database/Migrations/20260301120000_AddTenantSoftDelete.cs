using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PlatformPlatform.Account.Database.Migrations;

[DbContext(typeof(AccountDbContext))]
[Migration("20260301120000_AddTenantSoftDelete")]
public sealed class AddTenantSoftDelete : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>("DeletedAt", "Tenants", "datetimeoffset", nullable: true);
    }
}
