using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PlatformPlatform.BackOffice.Database;

[DbContext(typeof(BackOfficeDbContext))]
[Migration("20250217000000_Initial")]
public sealed class DatabaseMigrations : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
    }
}
