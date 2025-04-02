using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PlatformPlatform.BackOffice.Database.Migrations;

[DbContext(typeof(BackOfficeDbContext))]
[Migration("20250217000000_Initial")]
public sealed class Initial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
    }
}
