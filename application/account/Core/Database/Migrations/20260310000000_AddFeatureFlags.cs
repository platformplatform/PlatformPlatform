using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Account.Database.Migrations;

[DbContext(typeof(AccountDbContext))]
[Migration("20260310000000_AddFeatureFlags")]
public sealed class AddFeatureFlags : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "feature_flags",
            table => new
            {
                tenant_id = table.Column<long>("bigint", nullable: true),
                id = table.Column<string>("varchar(32)", nullable: false),
                flag_key = table.Column<string>("varchar(50)", nullable: false),
                user_id = table.Column<string>("varchar(32)", nullable: true),
                created_at = table.Column<DateTimeOffset>("timestamptz", nullable: false),
                modified_at = table.Column<DateTimeOffset>("timestamptz", nullable: true),
                enabled_at = table.Column<DateTimeOffset>("timestamptz", nullable: true),
                disabled_at = table.Column<DateTimeOffset>("timestamptz", nullable: true),
                bucket_start = table.Column<int>("integer", nullable: true),
                bucket_end = table.Column<int>("integer", nullable: true),
                configurable_by_tenant = table.Column<bool>("boolean", nullable: false, defaultValue: false),
                configurable_by_user = table.Column<bool>("boolean", nullable: false, defaultValue: false)
            },
            constraints: table => { table.PrimaryKey("pk_feature_flags", x => x.id); }
        );

        migrationBuilder.Sql(
            "CREATE UNIQUE INDEX ix_feature_flags_flag_key_tenant_id_user_id ON feature_flags (flag_key, tenant_id, user_id) NULLS NOT DISTINCT"
        );

        migrationBuilder.Sql(
            "ALTER TABLE feature_flags ADD CONSTRAINT ck_feature_flags_user_requires_tenant CHECK (user_id IS NULL OR tenant_id IS NOT NULL)"
        );

        migrationBuilder.Sql(
            """
            ALTER TABLE feature_flags ADD CONSTRAINT ck_feature_flags_bucket_range
            CHECK ((bucket_start IS NULL) = (bucket_end IS NULL) AND (bucket_start IS NULL OR (bucket_start BETWEEN 1 AND 100 AND bucket_end BETWEEN 1 AND 100)))
            """
        );

        migrationBuilder.AddColumn<short>("rollout_bucket", "tenants", "smallint", nullable: true);
        migrationBuilder.Sql("UPDATE tenants SET rollout_bucket = floor(random() * 100 + 1)::smallint WHERE rollout_bucket IS NULL");
        migrationBuilder.Sql("ALTER TABLE tenants ALTER COLUMN rollout_bucket SET NOT NULL");

        migrationBuilder.AddColumn<short>("rollout_bucket", "users", "smallint", nullable: true);
        migrationBuilder.Sql("UPDATE users SET rollout_bucket = floor(random() * 100 + 1)::smallint WHERE rollout_bucket IS NULL");
        migrationBuilder.Sql("ALTER TABLE users ALTER COLUMN rollout_bucket SET NOT NULL");
    }
}
