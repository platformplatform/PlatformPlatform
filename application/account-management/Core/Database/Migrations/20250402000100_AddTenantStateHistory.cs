using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PlatformPlatform.AccountManagement.Database.Migrations;

[DbContext(typeof(AccountManagementDbContext))]
[Migration("20250402000100_AddTenantStateHistory")]
public sealed class AddTenantStateHistory : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            "StateHistory",
            "Tenants",
            "nvarchar(max)",
            nullable: false,
            defaultValue: """[{"Ordinal": 0,"ChangedAt": "2025-01-01T00:00:00","State": "Trial","ChangedByEmail": "migration"}]"""
        );
    }
}
