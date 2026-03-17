using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Main.Database.Migrations;

[DbContext(typeof(MainDbContext))]
[Migration("20260125035900_Initial")]
public sealed class Initial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "__data_migrations_history",
            table => new
            {
                migration_id = table.Column<string>("text", nullable: false),
                product_version = table.Column<string>("text", nullable: false),
                executed_at = table.Column<DateTimeOffset>("timestamptz", nullable: false),
                execution_time_ms = table.Column<long>("bigint", nullable: false),
                summary = table.Column<string>("text", nullable: false)
            },
            constraints: table => { table.PrimaryKey("pk___data_migrations_history", x => x.migration_id); }
        );
    }
}
