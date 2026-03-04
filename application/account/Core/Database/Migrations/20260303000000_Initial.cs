using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Account.Database.Migrations;

[DbContext(typeof(AccountDbContext))]
[Migration("20260303000000_Initial")]
public sealed class Initial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "tenants",
            table => new
            {
                id = table.Column<string>("bigint", nullable: false),
                created_at = table.Column<DateTimeOffset>("timestamptz", nullable: false),
                modified_at = table.Column<DateTimeOffset>("timestamptz", nullable: true),
                deleted_at = table.Column<DateTimeOffset>("timestamptz", nullable: true),
                name = table.Column<string>("varchar(30)", nullable: false),
                state = table.Column<string>("varchar(20)", nullable: false),
                logo = table.Column<string>("varchar(150)", nullable: false, defaultValue: "{}"),
                suspension_reason = table.Column<string>("varchar(30)", nullable: true),
                suspended_at = table.Column<DateTimeOffset>("timestamptz", nullable: true),
                plan = table.Column<string>("varchar(10)", nullable: false, defaultValue: "Basis")
            },
            constraints: table => { table.PrimaryKey("pk_tenants", x => x.id); }
        );

        migrationBuilder.CreateTable(
            "email_logins",
            table => new
            {
                id = table.Column<string>("varchar(32)", nullable: false),
                created_at = table.Column<DateTimeOffset>("timestamptz", nullable: false),
                modified_at = table.Column<DateTimeOffset>("timestamptz", nullable: true),
                type = table.Column<string>("varchar(20)", nullable: false),
                email = table.Column<string>("varchar(100)", nullable: false),
                one_time_password_hash = table.Column<string>("char(84)", nullable: false),
                retry_count = table.Column<int>("integer", nullable: false),
                resend_count = table.Column<int>("integer", nullable: false),
                completed = table.Column<bool>("boolean", nullable: false)
            },
            constraints: table => { table.PrimaryKey("pk_email_logins", x => x.id); }
        );

        migrationBuilder.CreateIndex("ix_email_logins_email", "email_logins", "email");

        migrationBuilder.CreateTable(
            "external_logins",
            table => new
            {
                id = table.Column<string>("varchar(32)", nullable: false),
                created_at = table.Column<DateTimeOffset>("timestamptz", nullable: false),
                modified_at = table.Column<DateTimeOffset>("timestamptz", nullable: true),
                type = table.Column<string>("varchar(20)", nullable: false),
                provider_type = table.Column<string>("varchar(20)", nullable: false),
                email = table.Column<string>("varchar(100)", nullable: true),
                code_verifier = table.Column<string>("char(128)", nullable: false),
                nonce = table.Column<string>("char(43)", nullable: false),
                browser_fingerprint = table.Column<string>("char(64)", nullable: false),
                login_result = table.Column<string>("varchar(30)", nullable: true)
            },
            constraints: table => { table.PrimaryKey("pk_external_logins", x => x.id); }
        );

        migrationBuilder.CreateTable(
            "users",
            table => new
            {
                tenant_id = table.Column<long>("bigint", nullable: false),
                id = table.Column<string>("varchar(32)", nullable: false),
                created_at = table.Column<DateTimeOffset>("timestamptz", nullable: false),
                modified_at = table.Column<DateTimeOffset>("timestamptz", nullable: true),
                deleted_at = table.Column<DateTimeOffset>("timestamptz", nullable: true),
                last_seen_at = table.Column<DateTimeOffset>("timestamptz", nullable: true),
                email = table.Column<string>("varchar(100)", nullable: false),
                external_identities = table.Column<string>("text", nullable: false, defaultValue: "[]"),
                email_confirmed = table.Column<bool>("boolean", nullable: false),
                first_name = table.Column<string>("varchar(30)", nullable: true),
                last_name = table.Column<string>("varchar(30)", nullable: true),
                title = table.Column<string>("varchar(50)", nullable: true),
                role = table.Column<string>("varchar(20)", nullable: false),
                locale = table.Column<string>("varchar(5)", nullable: false),
                avatar = table.Column<string>("varchar(150)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_users", x => x.id);
                table.ForeignKey("fk_users_tenants_tenant_id", x => x.tenant_id, "tenants", "id");
            }
        );

        migrationBuilder.CreateIndex("ix_users_tenant_id", "users", "tenant_id");
        migrationBuilder.CreateIndex("ix_users_tenant_id_email", "users", ["tenant_id", "email"], unique: true, filter: "deleted_at IS NULL");

        migrationBuilder.CreateTable(
            "sessions",
            table => new
            {
                tenant_id = table.Column<long>("bigint", nullable: false),
                id = table.Column<string>("varchar(32)", nullable: false),
                user_id = table.Column<string>("varchar(32)", nullable: false),
                created_at = table.Column<DateTimeOffset>("timestamptz", nullable: false),
                modified_at = table.Column<DateTimeOffset>("timestamptz", nullable: true),
                refresh_token_jti = table.Column<string>("varchar(32)", nullable: false),
                previous_refresh_token_jti = table.Column<string>("varchar(32)", nullable: true),
                refresh_token_version = table.Column<int>("integer", nullable: false),
                login_method = table.Column<string>("varchar(20)", nullable: false),
                device_type = table.Column<string>("varchar(20)", nullable: false),
                user_agent = table.Column<string>("varchar(500)", nullable: false),
                ip_address = table.Column<string>("varchar(45)", nullable: false),
                revoked_at = table.Column<DateTimeOffset>("timestamptz", nullable: true),
                revoked_reason = table.Column<string>("varchar(20)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_sessions", x => x.id);
                table.ForeignKey("fk_sessions_tenants_tenant_id", x => x.tenant_id, "tenants", "id");
                table.ForeignKey("fk_sessions_users_user_id", x => x.user_id, "users", "id", onDelete: ReferentialAction.Cascade);
            }
        );

        migrationBuilder.CreateIndex("ix_sessions_tenant_id", "sessions", "tenant_id");
        migrationBuilder.CreateIndex("ix_sessions_user_id", "sessions", "user_id");

        migrationBuilder.CreateTable(
            "subscriptions",
            table => new
            {
                tenant_id = table.Column<long>("bigint", nullable: false),
                id = table.Column<string>("varchar(32)", nullable: false),
                created_at = table.Column<DateTimeOffset>("timestamptz", nullable: false),
                modified_at = table.Column<DateTimeOffset>("timestamptz", nullable: true),
                plan = table.Column<string>("varchar(20)", nullable: false),
                scheduled_plan = table.Column<string>("varchar(20)", nullable: true),
                stripe_customer_id = table.Column<string>("varchar(32)", nullable: true),
                stripe_subscription_id = table.Column<string>("varchar(32)", nullable: true),
                current_price_amount = table.Column<decimal>("numeric(18,2)", nullable: true),
                current_price_currency = table.Column<string>("varchar(3)", nullable: true),
                current_period_end = table.Column<DateTimeOffset>("timestamptz", nullable: true),
                cancel_at_period_end = table.Column<bool>("boolean", nullable: false),
                first_payment_failed_at = table.Column<DateTimeOffset>("timestamptz", nullable: true),
                cancellation_reason = table.Column<string>("varchar(20)", nullable: true),
                cancellation_feedback = table.Column<string>("varchar(500)", nullable: true),
                payment_transactions = table.Column<string>("text", nullable: false),
                payment_method = table.Column<string>("text", nullable: true),
                billing_info = table.Column<string>("text", nullable: true)
            },
            constraints: table => { table.PrimaryKey("pk_subscriptions", x => x.id); }
        );

        migrationBuilder.CreateIndex("ix_subscriptions_tenant_id", "subscriptions", "tenant_id", unique: true);
        migrationBuilder.CreateIndex("ix_subscriptions_stripe_customer_id", "subscriptions", "stripe_customer_id", unique: true, filter: "stripe_customer_id IS NOT NULL");

        migrationBuilder.CreateTable(
            "stripe_events",
            table => new
            {
                tenant_id = table.Column<long>("bigint", nullable: true),
                id = table.Column<string>("varchar(32)", nullable: false),
                created_at = table.Column<DateTimeOffset>("timestamptz", nullable: false),
                modified_at = table.Column<DateTimeOffset>("timestamptz", nullable: true),
                event_type = table.Column<string>("varchar(50)", nullable: false),
                status = table.Column<string>("varchar(20)", nullable: false),
                processed_at = table.Column<DateTimeOffset>("timestamptz", nullable: true),
                stripe_customer_id = table.Column<string>("varchar(32)", nullable: true),
                stripe_subscription_id = table.Column<string>("varchar(32)", nullable: true),
                payload = table.Column<string>("text", nullable: true),
                error = table.Column<string>("varchar(500)", nullable: true)
            },
            constraints: table => { table.PrimaryKey("pk_stripe_events", x => x.id); }
        );

        migrationBuilder.CreateIndex("ix_stripe_events_tenant_id", "stripe_events", "tenant_id");
        migrationBuilder.CreateIndex("ix_stripe_events_stripe_customer_id_status", "stripe_events", ["stripe_customer_id", "status"]);
    }
}
