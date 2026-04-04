using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Account.Database.Migrations;

[DbContext(typeof(AccountDbContext))]
[Migration("20260404100000_AddSourceToFeatureFlags")]
public sealed class AddSourceToFeatureFlags : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("source", "feature_flags", "text", nullable: false, defaultValue: "Manual");

        migrationBuilder.Sql(
            """
            UPDATE feature_flags
            SET source = 'AbRollout'
            WHERE bucket_start IS NOT NULL AND bucket_end IS NOT NULL AND tenant_id IS NULL AND user_id IS NULL
            """
        );
    }
}
