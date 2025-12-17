---
paths: **/Domain/*.cs
description: Rules for creating strongly typed IDs for DDD aggregates and entities
---

# Strongly Typed IDs

Guidelines for implementing strongly typed IDs in the backend, covering type safety, naming, serialization, and EF Core mapping.

## Implementation

1. Use strongly typed IDs to provide type safety and prevent mixing different ID types, improving readability and maintainability
2. By default, use `StronglyTypedUlid<T>` as the base class—it provides chronological ordering and includes a prefix for easy recognition (e.g., `usr_01JMVAW4T4320KJ3A7EJMCG8R0`)
3. Use the `[IdPrefix]` attribute with a short prefix (max 5 characters)—ULIDs are 26 chars, plus 5-char prefix and underscore = 32 chars for varchar(32)
4. Follow the naming convention `[Entity]Id`
5. Include the `[JsonConverter]` attribute for proper serialization
6. Always override `ToString()` in the concrete class—record types don't inherit this from the base class
7. Place the ID class in the same file as its corresponding aggregate or entity
8. Use strongly typed IDs everywhere: API endpoints, DTOs, commands, queries, and the frontend webapp
9. In rare cases, other ID types can be used for performance (e.g., `TenantId` uses `long` because it's faster and used in almost every table)
10. `UserId` and `TenantId` are shared between self-contained systems, so they're defined in the shared kernel
11. Map strongly typed IDs in EF Core configurations using:
    - `MapStronglyTypedUuid` for ULIDs
    - `MapStronglyTypedLongId` for long IDs
    - `MapStronglyTypedGuid` for GUIDs

## Examples

### Example 1 - UserId (Using default StronglyTypedUlid)

```csharp
// ✅ DO: Use StronglyTypedUlid with prefix and proper serialization
[PublicAPI]
[IdPrefix("usr")]
[JsonConverter(typeof(StronglyTypedIdJsonConverter<string, UserId>))]
public sealed record UserId(string Value) : StronglyTypedUlid<UserId>(Value)
{
    public override string ToString()
    {
        return Value;
    }
}

// ❌ DON'T: Forget to override ToString or use incorrect naming
public sealed record BadUserIdentifier(string Value) : StronglyTypedUlid<BadUserIdentifier>(Value)
{
    // Missing ToString override
    // Incorrect naming - should be UserId, not UserIdentifier
}
```

### Example 2 - TenantId (Using StronglyTypedLongId for performance)

```csharp
// ✅ DO: Use StronglyTypedLongId for performance-critical IDs
[PublicAPI]
[JsonConverter(typeof(StronglyTypedIdJsonConverter<long, TenantId>))]
public sealed record TenantId(long Value) : StronglyTypedLongId<TenantId>(Value)
{
    public override string ToString()
    {
        return Value.ToString();
    }
}

// ❌ DON'T: Use primitive types directly
public class BadUser
{
    // Wrong: using primitive types directly instead of strongly typed IDs
    public string Id { get; set; } // Should be UserId
    public long TenantId { get; set; } // Should be TenantId
}
```

### Example 3 - Entity Framework Core Mapping

```csharp
// ✅ DO: Map strongly typed IDs in Entity Framework Core configurations
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.MapStronglyTypedUuid<User, UserId>(u => u.Id);
        builder.MapStronglyTypedLongId<User, TenantId>(u => u.TenantId);
    }
}

// ❌ DON'T: Use manual conversions for strongly typed IDs
public sealed class BadUserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Wrong: manual conversion instead of using extension methods
        builder.Property(u => u.Id).HasConversion(
            id => id.Value,
            value => new UserId(value));
    }
}
```
