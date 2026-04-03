using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Account.Database.Migrations;

[DbContext(typeof(AccountDbContext))]
[Migration("20260402000000_AddFeatureFlagVersionToTenants")]
public sealed class AddFeatureFlagVersionToTenants : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>("feature_flag_version", "tenants", "integer", nullable: false, defaultValue: 0);
    }
}
