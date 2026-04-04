using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Account.Database.Migrations;

[DbContext(typeof(AccountDbContext))]
[Migration("20260404000000_ReplaceRolloutBucketsWithVanDerCorput")]
public sealed class ReplaceRolloutBucketsWithVanDerCorput : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create a temporary function to compute van der Corput buckets in SQL
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

        // Add rollout_bucket_sequence to tenants
        migrationBuilder.AddColumn<int>("rollout_bucket_sequence", "tenants", "integer", nullable: true);

        migrationBuilder.Sql(
            """
            WITH numbered AS (
                SELECT id, row_number() OVER (ORDER BY created_at, id) - 1 AS seq
                FROM tenants
            )
            UPDATE tenants SET rollout_bucket_sequence = numbered.seq
            FROM numbered WHERE tenants.id = numbered.id
            """
        );

        migrationBuilder.Sql("ALTER TABLE tenants ALTER COLUMN rollout_bucket_sequence SET NOT NULL");

        // Recompute rollout_bucket using van der Corput
        migrationBuilder.Sql("UPDATE tenants SET rollout_bucket = van_der_corput_bucket(rollout_bucket_sequence)");

        // Add rollout_bucket_sequence to users
        migrationBuilder.AddColumn<int>("rollout_bucket_sequence", "users", "integer", nullable: true);

        migrationBuilder.Sql(
            """
            WITH numbered AS (
                SELECT id, row_number() OVER (ORDER BY created_at, id) - 1 AS seq
                FROM users
            )
            UPDATE users SET rollout_bucket_sequence = numbered.seq
            FROM numbered WHERE users.id = numbered.id
            """
        );

        migrationBuilder.Sql("ALTER TABLE users ALTER COLUMN rollout_bucket_sequence SET NOT NULL");

        // Recompute rollout_bucket using van der Corput
        migrationBuilder.Sql("UPDATE users SET rollout_bucket = van_der_corput_bucket(rollout_bucket_sequence)");

        // Drop the temporary function
        migrationBuilder.Sql("DROP FUNCTION van_der_corput_bucket(integer)");

        // Update bucket_range constraint from 1-100 to 0-99
        migrationBuilder.Sql("ALTER TABLE feature_flags DROP CONSTRAINT ck_feature_flags_bucket_range");

        migrationBuilder.Sql(
            """
            ALTER TABLE feature_flags ADD CONSTRAINT ck_feature_flags_bucket_range
            CHECK ((bucket_start IS NULL) = (bucket_end IS NULL) AND (bucket_start IS NULL OR (bucket_start BETWEEN 0 AND 99 AND bucket_end BETWEEN 0 AND 99)))
            """
        );

        // Update any existing rollout ranges that used bucket 100 to use 99
        migrationBuilder.Sql("UPDATE feature_flags SET bucket_end = 99 WHERE bucket_end = 100");
        migrationBuilder.Sql("UPDATE feature_flags SET bucket_start = 99 WHERE bucket_start = 100");
    }
}
