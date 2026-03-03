---
trigger: glob
globs: **/Database/Migrations/*.cs
description: Rules for creating database migrations with PostgreSQL conventions
---

# Database Migrations

Guidelines for creating database migrations using PostgreSQL conventions with snake_case naming.

## Implementation

1. Create migrations manually rather than using Entity Framework tooling:
   - Place migrations in `/[scs-name]/Core/Database/Migrations`
   - Name migration files with 14-digit timestamp prefix: `YYYYMMDDHHmmss_MigrationName.cs`
   - Only implement the `Up` method--don't create `Down` migration

2. Follow this strict column ordering in table creation statements:
   - `tenant_id` (if applicable)
   - `id` (always required)
   - Foreign keys (if applicable)
   - `created_at` and `modified_at` as non-nullable/nullable `timestamptz`
   - All other properties in the same order as they appear in the C# Aggregate class

3. Use snake_case naming for everything:
   - Table names: plural, lowercase (e.g., `users`, `email_logins`, `stripe_events`)
   - Column names: lowercase with underscores (e.g., `tenant_id`, `created_at`, `email_confirmed`)
   - C# anonymous type members must also be snake_case (e.g., `tenant_id = table.Column<long>(...)`)
   - Constraint names: `pk_table_name`, `fk_child_table_parent_table_column_name`, `ix_table_name_column_name`

4. Use appropriate PostgreSQL data types:
   - Use `varchar(32)` for strongly typed IDs (ULID is 26 chars + underscore + max 5-char prefix = exactly 32)
   - Use `varchar(N)` for all text columns--deduce size from validators, enum values, or property semantics
   - Use `timestamptz` for `DateTimeOffset` columns--never `timestamp` or `datetime`
   - Use `boolean` for bool properties
   - Use `integer` for int properties
   - Use `bigint` for long properties (e.g., `TenantId`)
   - Use `numeric(18,2)` for decimal properties and always add `HasPrecision(18, 2)` in the EF Core configuration
   - Use `text` for unbounded string or JSON data
   - Default to `varchar(10)` or `varchar(20)` for enum values

5. Create appropriate constraints and indexes:
   - Primary keys: `pk_table_name`
   - Foreign keys: `fk_child_table_parent_table_column_name`
   - Indexes: `ix_table_name_column_name`
   - Filtered indexes use PostgreSQL `WHERE` clause syntax (e.g., `filter: "deleted_at IS NULL"`)

6. Migrate existing data:
   - Use `migrationBuilder.Sql("UPDATE table_name SET column_name = value WHERE condition")` with care

## Examples

### Example 1 - Simple table

```csharp
[DbContext(typeof(AccountDbContext))]
[Migration("20250507141500_AddUserPreferences")] // ✅ DO: Use 14-digit timestamp
public sealed class AddUserPreferences : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "user_preferences", // ✅ DO: Use snake_case plural table name
            table => new
            {
                tenant_id = table.Column<long>("bigint", nullable: false), // ✅ DO: Add tenant_id as first column
                id = table.Column<string>("varchar(32)", nullable: false), // ✅ DO: Make id varchar(32)
                user_id = table.Column<string>("varchar(32)", nullable: false), // ✅ DO: Add foreign key before created_at/modified_at
                created_at = table.Column<DateTimeOffset>("timestamptz", nullable: false), // ✅ DO: Use timestamptz
                modified_at = table.Column<DateTimeOffset>("timestamptz", nullable: true),
                language = table.Column<string>("varchar(10)", nullable: false) // ✅ DO: Use varchar for known values
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_user_preferences", x => x.id); // ✅ DO: Use pk_table_name
                table.ForeignKey("fk_user_preferences_users_user_id", x => x.user_id, "users", "id"); // ✅ DO: Use fk_child_parent_column
            }
        );

        migrationBuilder.CreateIndex("ix_user_preferences_tenant_id", "user_preferences", "tenant_id"); // ✅ DO: Use ix_table_column
        migrationBuilder.CreateIndex("ix_user_preferences_user_id", "user_preferences", "user_id");
    }
}

// ❌ DON'T: Forget to add the attribute [DbContext(typeof(XxxDbContext))] for the self-contained system
[Migration("20250507_AddUserPrefs")]  // ❌ Missing proper 14-digit timestamp
public class AddUserPrefsMigration : Migration  // ❌ Not sealed, incorrect naming, suffixed with Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "UserPreference",   // ❌ DON'T: Use PascalCase or singular name for table
            table => new
            {
                Id = table.Column<string>("varchar(30)", nullable: false), // ❌ DON'T: Use PascalCase column names or wrong varchar size
                Theme = table.Column<string>("varchar(20)", nullable: false),  // ❌ DON'T: Add properties before created_at/modified_at
                TenantId = table.Column<long>("bigint", nullable: false),  // ❌ tenant_id should be first, and snake_case
                CreatedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: false), // ❌ DON'T: Use SQL Server types
                ModifiedAt = table.Column<DateTimeOffset>("datetime", nullable: true), // ❌ DON'T: Use datetime
                UserId = table.Column<string>("varchar(32)", nullable: false),  // ❌ Foreign key after created_at/modified_at
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserPreference", i => i.Id);  // ❌ PascalCase PK naming, variable should be x not i
                table.ForeignKey("FK_UserPreference_User", x => x.UserId, "Users", "Id");  // ❌ PascalCase FK naming
            }
        );
    }

    protected override void Down(MigrationBuilder migrationBuilder)  // ❌ DON'T: Create a down method
    {
        migrationBuilder.DropTable("UserPreference");
    }
}
```

### Example 2 - Filtered indexes and data migrations

```csharp
// ✅ DO: Use PostgreSQL WHERE clause syntax for filtered indexes
migrationBuilder.CreateIndex("ix_users_tenant_id_email", "users", ["tenant_id", "email"], unique: true, filter: "deleted_at IS NULL");
migrationBuilder.CreateIndex("ix_subscriptions_stripe_customer_id", "subscriptions", "stripe_customer_id", unique: true, filter: "stripe_customer_id IS NOT NULL");

// ❌ DON'T: Use SQL Server bracket notation
migrationBuilder.CreateIndex("IX_Users_TenantId_Email", "Users", ["TenantId", "Email"], unique: true, filter: "[DeletedAt] IS NULL");
```

```csharp
// ✅ DO: Match column size to validator, use snake_case
public sealed class UpdateUserValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.TimeZone).NotEmpty().MaximumLength(50);
    }
}

protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AddColumn<string>("time_zone", "users", "varchar(50)", nullable: false, defaultValue: "UTC");
}
```
