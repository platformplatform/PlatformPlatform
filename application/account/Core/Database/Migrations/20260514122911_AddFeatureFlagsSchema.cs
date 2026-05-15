using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Account.Database.Migrations;

[DbContext(typeof(AccountDbContext))]
[Migration("20260514122911_AddFeatureFlagsSchema")]
public sealed class AddFeatureFlagsSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "feature_flags",
            table => new
            {
                tenant_id = table.Column<long>("bigint", nullable: true),
                id = table.Column<string>("text", nullable: false),
                user_id = table.Column<string>("text", nullable: true),
                created_at = table.Column<DateTimeOffset>("timestamptz", nullable: false),
                modified_at = table.Column<DateTimeOffset>("timestamptz", nullable: true),
                deleted_at = table.Column<DateTimeOffset>("timestamptz", nullable: true),
                orphaned_at = table.Column<DateTimeOffset>("timestamptz", nullable: true),
                flag_key = table.Column<string>("text", nullable: false),
                enabled_at = table.Column<DateTimeOffset>("timestamptz", nullable: true),
                disabled_at = table.Column<DateTimeOffset>("timestamptz", nullable: true),
                bucket_start = table.Column<int>("integer", nullable: true),
                bucket_end = table.Column<int>("integer", nullable: true),
                source = table.Column<string>("text", nullable: false),
                scope = table.Column<string>("text", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_feature_flags", x => x.id);
                table.ForeignKey("fk_feature_flags_users_user_id", x => x.user_id, "users", "id", onDelete: ReferentialAction.Cascade);
            }
        );

        migrationBuilder.Sql(
            "CREATE UNIQUE INDEX ix_feature_flags_flag_key_tenant_id_user_id ON feature_flags (flag_key, tenant_id, user_id) NULLS NOT DISTINCT;"
        );

        // Indexes for the hot evaluator query paths. The unique index above leads on flag_key, which
        // cannot serve the tenant-scoped / user-scoped lookups the evaluator runs on every JWT refresh.
        migrationBuilder.CreateIndex("ix_feature_flags_tenant_id", "feature_flags", "tenant_id");
        migrationBuilder.CreateIndex("ix_feature_flags_user_id", "feature_flags", "user_id", filter: "user_id IS NOT NULL");
        migrationBuilder.CreateIndex(
            "ix_feature_flags_tenant_id_source",
            "feature_flags",
            ["tenant_id", "source"],
            filter: "source = 'Plan' AND user_id IS NULL"
        );

        migrationBuilder.Sql(
            "ALTER TABLE feature_flags ADD CONSTRAINT ck_feature_flags_user_requires_tenant CHECK (user_id IS NULL OR tenant_id IS NOT NULL);"
        );

        migrationBuilder.Sql(
            """
            ALTER TABLE feature_flags ADD CONSTRAINT ck_feature_flags_bucket_range
            CHECK ((bucket_start IS NULL) = (bucket_end IS NULL) AND (bucket_start IS NULL OR (bucket_start BETWEEN 0 AND 99 AND bucket_end BETWEEN 0 AND 99)));
            """
        );

        // Add rollout_bucket to tenants and users, computed via van der Corput sequence so existing rows
        // are evenly spread across 0..99 for low-percent rollout fairness.
        migrationBuilder.AddColumn<short>("rollout_bucket", "tenants", "smallint", nullable: true);
        migrationBuilder.AddColumn<short>("rollout_bucket", "users", "smallint", nullable: true);

        // Stored as text rather than an enum-by-int so the JSON wire value (AlwaysOn / NeverOn) round-trips
        // through EF's enum-to-string conversion and remains readable in raw SQL inspections.
        migrationBuilder.AddColumn<string>("ab_inclusion_pin", "tenants", "text", nullable: true);
        migrationBuilder.AddColumn<string>("ab_inclusion_pin", "users", "text", nullable: true);

        migrationBuilder.Sql(
            "ALTER TABLE tenants ADD CONSTRAINT ck_tenants_ab_inclusion_pin CHECK (ab_inclusion_pin IS NULL OR ab_inclusion_pin IN ('AlwaysOn', 'NeverOn'));"
        );
        migrationBuilder.Sql(
            "ALTER TABLE users ADD CONSTRAINT ck_users_ab_inclusion_pin CHECK (ab_inclusion_pin IS NULL OR ab_inclusion_pin IN ('AlwaysOn', 'NeverOn'));"
        );

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
            UPDATE tenants SET rollout_bucket = van_der_corput_bucket(numbered.seq::integer)
            FROM numbered WHERE tenants.id = numbered.id;
            """
        );

        migrationBuilder.Sql("ALTER TABLE tenants ALTER COLUMN rollout_bucket SET NOT NULL;");

        migrationBuilder.Sql(
            """
            WITH numbered AS (
                SELECT id, row_number() OVER (ORDER BY created_at, id) - 1 AS seq
                FROM users
            )
            UPDATE users SET rollout_bucket = van_der_corput_bucket(numbered.seq::integer)
            FROM numbered WHERE users.id = numbered.id;
            """
        );

        migrationBuilder.Sql("ALTER TABLE users ALTER COLUMN rollout_bucket SET NOT NULL;");

        migrationBuilder.Sql("DROP FUNCTION van_der_corput_bucket(integer);");

        // Postgres sequences hand out monotonically increasing values atomically across concurrent transactions;
        // a COUNT(*)-based index would lose uniqueness under parallel CreateTenantCommand / CreateUserCommand.
        // Seeded from the row count so the next index is strictly greater than any previously assigned,
        // preserving the low-discrepancy bucket spread for already-created rows.
        // SQLite (test database) has no sequence support; the repository falls back to COUNT(*) on that provider.
        migrationBuilder.Sql(
            "DO $$ DECLARE start_value bigint; BEGIN " +
            "SELECT COUNT(*) + 1 INTO start_value FROM tenants; " +
            "EXECUTE format('CREATE SEQUENCE IF NOT EXISTS tenant_rollout_index_sequence AS bigint START WITH %s', start_value); " +
            "END $$;"
        );
        migrationBuilder.Sql(
            "DO $$ DECLARE start_value bigint; BEGIN " +
            "SELECT COUNT(*) + 1 INTO start_value FROM users; " +
            "EXECUTE format('CREATE SEQUENCE IF NOT EXISTS user_rollout_index_sequence AS bigint START WITH %s', start_value); " +
            "END $$;"
        );
    }
}
