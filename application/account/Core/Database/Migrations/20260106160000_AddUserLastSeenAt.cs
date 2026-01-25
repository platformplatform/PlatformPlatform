using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PlatformPlatform.Account.Database;

namespace PlatformPlatform.Account.Database.Migrations;

[DbContext(typeof(AccountDbContext))]
[Migration("20260106160000_AddUserLastSeenAt")]
public sealed class AddUserLastSeenAt : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>("LastSeenAt", "Users", "datetimeoffset", nullable: true);
    }
}
