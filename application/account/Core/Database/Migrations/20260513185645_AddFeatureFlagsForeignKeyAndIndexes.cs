using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Account.Database.Migrations;

[DbContext(typeof(AccountDbContext))]
[Migration("20260513185645_AddFeatureFlagsForeignKeyAndIndexes")]
public sealed class AddFeatureFlagsForeignKeyAndIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddForeignKey(
            "fk_feature_flags_users_user_id",
            "feature_flags",
            "user_id",
            "users",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade
        );

        // Indexes for the hot evaluator query paths. The pre-existing unique index leads on flag_key, which
        // cannot serve the tenant-scoped / user-scoped lookups the evaluator runs.
        migrationBuilder.CreateIndex("ix_feature_flags_tenant_id", "feature_flags", "tenant_id");
        migrationBuilder.CreateIndex("ix_feature_flags_user_id", "feature_flags", "user_id", filter: "user_id IS NOT NULL");
        migrationBuilder.CreateIndex(
            "ix_feature_flags_tenant_id_source",
            "feature_flags",
            ["tenant_id", "source"],
            filter: "source = 'Plan' AND user_id IS NULL"
        );
    }
}
