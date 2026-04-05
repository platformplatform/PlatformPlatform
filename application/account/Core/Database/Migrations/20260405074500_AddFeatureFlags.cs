using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Account.Database.Migrations;

[DbContext(typeof(AccountDbContext))]
[Migration("20260405074500_AddFeatureFlags")]
public sealed class AddFeatureFlags : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "feature_flags",
            table => new
            {
                tenant_id = table.Column<long>("bigint", nullable: true),
                id = table.Column<string>("text", nullable: false),
                feature_flag_key = table.Column<string>("text", nullable: false),
                user_id = table.Column<string>("text", nullable: true),
                created_at = table.Column<DateTimeOffset>("timestamptz", nullable: false),
                modified_at = table.Column<DateTimeOffset>("timestamptz", nullable: true),
                enabled_at = table.Column<DateTimeOffset>("timestamptz", nullable: true),
                disabled_at = table.Column<DateTimeOffset>("timestamptz", nullable: true),
                rollout_bucket_start = table.Column<int>("integer", nullable: true),
                rollout_bucket_end = table.Column<int>("integer", nullable: true),
                configurable_by_tenant = table.Column<bool>("boolean", nullable: false, defaultValue: false),
                configurable_by_user = table.Column<bool>("boolean", nullable: false, defaultValue: false),
                source = table.Column<string>("text", nullable: false, defaultValue: "Manual")
            },
            constraints: table => { table.PrimaryKey("pk_feature_flags", x => x.id); }
        );

        migrationBuilder.Sql(
            "CREATE UNIQUE INDEX ix_feature_flags_feature_flag_key_tenant_id_user_id ON feature_flags (feature_flag_key, tenant_id, user_id) NULLS NOT DISTINCT"
        );

        migrationBuilder.Sql(
            "ALTER TABLE feature_flags ADD CONSTRAINT ck_feature_flags_user_requires_tenant CHECK (user_id IS NULL OR tenant_id IS NOT NULL)"
        );

        migrationBuilder.Sql(
            """
            ALTER TABLE feature_flags ADD CONSTRAINT ck_feature_flags_bucket_range
            CHECK ((rollout_bucket_start IS NULL) = (rollout_bucket_end IS NULL) AND (rollout_bucket_start IS NULL OR (rollout_bucket_start BETWEEN 0 AND 99 AND rollout_bucket_end BETWEEN 0 AND 99)))
            """
        );

        migrationBuilder.AddColumn<int>("feature_flag_version", "tenants", "integer", nullable: false, defaultValue: 0);

        // Add rollout_bucket to tenants and users, computed via van der Corput sequence
        migrationBuilder.AddColumn<short>("rollout_bucket", "tenants", "smallint", nullable: true);
        migrationBuilder.AddColumn<short>("rollout_bucket", "users", "smallint", nullable: true);

        migrationBuilder.Sql(
            """
            CREATE OR REPLACE FUNCTION van_der_corput_bucket(seq integer) RETURNS integer AS $$
            DECLARE
                result double precision := 0;
                denominator double precision := 2;
                n integer := seq;
            BEGIN
                WHILE n > 0 LOOP
                    result := result + (n & 1)::double precision / denominator;
                    n := n >> 1;
                    denominator := denominator * 2;
                END LOOP;
                RETURN floor(result * 100)::integer;
            END;
            $$ LANGUAGE plpgsql IMMUTABLE;
            """
        );

        migrationBuilder.Sql(
            """
            WITH numbered AS (
                SELECT id, row_number() OVER (ORDER BY created_at, id) - 1 AS seq
                FROM tenants
            )
            UPDATE tenants SET rollout_bucket = van_der_corput_bucket(numbered.seq)
            FROM numbered WHERE tenants.id = numbered.id
            """
        );

        migrationBuilder.Sql("ALTER TABLE tenants ALTER COLUMN rollout_bucket SET NOT NULL");

        migrationBuilder.Sql(
            """
            WITH numbered AS (
                SELECT id, row_number() OVER (ORDER BY created_at, id) - 1 AS seq
                FROM users
            )
            UPDATE users SET rollout_bucket = van_der_corput_bucket(numbered.seq)
            FROM numbered WHERE users.id = numbered.id
            """
        );

        migrationBuilder.Sql("ALTER TABLE users ALTER COLUMN rollout_bucket SET NOT NULL");

        migrationBuilder.Sql("DROP FUNCTION van_der_corput_bucket(integer)");
    }
}
