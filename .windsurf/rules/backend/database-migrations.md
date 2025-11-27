---
trigger: glob
globs: **/Database/Migrations/*.cs
description: Rules for creating database migrations
---

# Database Migrations

Carefully follow these instructions when creating database migrations.

## Implementation

1. Create migrations manually rather than using Entity Framework tooling:
   - Place migrations in the `/[scs-name]/Core/Database/Migrations` directory.
   - Name migration files with a 14-digit timestamp prefix in the format `YYYYMMDDHHmmss_MigrationName.cs`.
   - Only implement the `Up` method; do not implement the `Down` method.
   - I repeat... DO NOT CREATE `Down` migration.

2. Follow this strict column ordering in all table creation statements:
   - `TenantId` (if applicable)
   - `Id` (always required)
   - Foreign keys (if applicable)
   - `CreatedAt` and `ModifiedAt` as non-nullable `datetimeoffset`
   - All other properties in the exact same order as they appear in the C# Aggregate class

3. Use appropriate SQL Server data types:
   - For strongly typed IDs default to `varchar(32)` (a ULID is 26 characters, plus underscore and max 5 char prefix).
   - Intelligent deduct use of varchar or nvarchar based on the property type, and command validators, enum values, etc.
   - Use `datetimeoffset` (default), `datetime2` (timezone agnostic) or `date` (date only) and never use `datetime`.
   - Default to 'varchar(10)' or 'varchar(20)' for enum values.
4. Create appropriate constraints and indexes:
   - Define primary keys using the `PK_TableName` naming convention.
   - Define foreign keys using the `FK_ChildTable_ParentTable_ColumnName` naming convention.
   - Create indexes using the `IX_TableName_ColumnName` naming convention.

5. Migrate existing data:
   - Use `migrationBuilder.Sql("UPDATE [table] SET [column] = [value] WHERE [condition]")` to update data... but use with care.
6. Use standard SQL Server naming conventions:
   - Table names should be plural (e.g., `Users`, not `User`).
   - Constraint and index names should follow the patterns mentioned above.

## Examples

### Example 1 - Simple table

```csharp
[DbContext(typeof(AccountManagementDbContext))]
[Migration("20250507141500_AddUserPreferences")] // ✅ DO: Use 14-digit timestamp
public sealed class AddUserPreferences : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            "UserPreferences",
            table => new
            {
                TenantId = table.Column<long>("bigint", nullable: false), // ✅ DO: Add TenantId as first column
                Id = table.Column<string>("varchar(32)", nullable: false), // ✅ DO: Make Id varchar(32) by default
                UserId = table.Column<string>("varchar(32)", nullable: false), // ✅ DO: Add Foreginkey before CreatedAt/ModifiedAt
                CreatedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: false),
                ModifiedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: true),
                Language = table.Column<string>("varchar(10)", nullable: false) // ✅ DO: Use varchar when colum has known values
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserPreferences", x => x.Id);
                table.ForeignKey("FK_UserPreferences_Users_UserId", x => x.UserId, "Users", "Id");
            }
        );

        migrationBuilder.CreateIndex("IX_UserPreferences_TenantId", "UserPreferences", "TenantId");
        migrationBuilder.CreateIndex("IX_UserPreferences_UserId", "UserPreferences", "UserId");
    }
}

// ❌ DON'T: Forget to add the attribute [DbContext(typeof(XxxDbContext))] for the self-contained system 
[Migration("20250507_AddUserPrefs")]  // ❌ DON'T: Missing proper 14-digit timestamp
public class AddUserPrefsMigration : Migration  // ❌ DON'T: Not sealed and incorrect naming and suffix with Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create UserPreferences table  // ❌ DON'T: Do not add comments
        migrationBuilder.CreateTable(
            "UserPreference",   // ❌ DON'T: use singular name for table
            table => new
            {
                Id = table.Column<string>("varchar(30)", nullable: false), // ❌ DON'T: Use varchar(30) for ULID 
                Theme = table.Column<string>("varchar(20)", nullable: false),  /// ❌ DON'T: Add properties before CreatedAt/ModifiedAt 
                TenantId = table.Column<long>("bigint", nullable: false),  // ❌ DON'T: TenantId should be first
                CreatedAt = table.Column<DateTimeOffset>("datetimeoffset", nullable: false),
                ModifiedAt = table.Column<DateTimeOffset>("datetime", nullable: true), // ❌ DON'T: use datetime
                UserId = table.Column<string>("varchar(32)", nullable: false),  // ❌ DON'T: Foreign key after after CreatedAt/ModifiedAt
                Language = table.Column<string>("varchar(10)", nullable: false), // ❌ DON'T: Ending with a trailing comma
            },
            constraints: table =>
            {
                table.PrimaryKey("PrimaryKey_UserPreference", i => i.Id);  // ❌ DON'T: Incorrect PK naming, and the variable name should be x and not i
                table.ForeignKey("ForeignKey_UserPreference_User", x => x.UserId, "Users", "Id");  // ❌ DON'T: Incorrect FK naming
            }
        );
    }
    
    protected override void Down(MigrationBuilder migrationBuilder)  // ❌ DON'T: Create a down method
    {
        migrationBuilder.DropTable("UserPreference");
    }
}
```

### Example 2 - Determining column sizes from validators

```csharp
public sealed class UpdateUserValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.TimeZone).NotEmpty().MaximumLength(50); // ✅ DO: Use column sizes based on command validators
    }
}

protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AddColumn<string>("TimeZone", "Users", "varchar(50)", nullable: false, defaultValue: "UTC"); // ✅ DO: Match column size to validator
    // ✅ DO: Consider running complex logic here to update existing records
}
```
