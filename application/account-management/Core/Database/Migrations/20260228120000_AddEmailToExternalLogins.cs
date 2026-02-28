using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PlatformPlatform.AccountManagement.Database.Migrations;

[DbContext(typeof(AccountManagementDbContext))]
[Migration("20260228120000_AddEmailToExternalLogins")]
public sealed class AddEmailToExternalLogins : Migration
{
    // Email was later added to 20260210103500_AddExternalLogins, so new databases already have this column.
    // This migration handles databases where AddExternalLogins ran before the column was added.
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF NOT EXISTS (
                SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = 'ExternalLogins' AND COLUMN_NAME = 'Email'
            )
            BEGIN
                ALTER TABLE [ExternalLogins] ADD [Email] varchar(100) NULL;
            END
            """);
    }
}
