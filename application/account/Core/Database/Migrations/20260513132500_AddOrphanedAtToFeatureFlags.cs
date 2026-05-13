using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Account.Database.Migrations;

[DbContext(typeof(AccountDbContext))]
[Migration("20260513132500_AddOrphanedAtToFeatureFlags")]
public sealed class AddOrphanedAtToFeatureFlags : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTimeOffset>("orphaned_at", "feature_flags", "timestamptz", nullable: true);
    }
}
