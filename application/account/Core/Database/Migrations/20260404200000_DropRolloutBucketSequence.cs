using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Account.Database.Migrations;

[DbContext(typeof(AccountDbContext))]
[Migration("20260404200000_DropRolloutBucketSequence")]
public sealed class DropRolloutBucketSequence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn("rollout_bucket_sequence", "tenants");
        migrationBuilder.DropColumn("rollout_bucket_sequence", "users");
    }
}
