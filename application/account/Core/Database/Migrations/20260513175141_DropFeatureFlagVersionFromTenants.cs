using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Account.Database.Migrations;

[DbContext(typeof(AccountDbContext))]
[Migration("20260513175141_DropFeatureFlagVersionFromTenants")]
public sealed class DropFeatureFlagVersionFromTenants : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("feature_flag_version", "tenants");
    }
}
