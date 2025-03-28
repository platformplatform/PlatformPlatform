# Strongly Typed IDs

When implementing strongly typed IDs, follow these rules very carefully.

## Purpose

Strongly typed IDs provide type safety and prevent mixing different ID types that are technically the same primitive type. They also improve readability and maintainability by making the code more self-documenting.

## Implementation Guidelines

1. By default, use `StronglyTypedUlid<T>` as the base class for IDs as it provides chronological ordering and includes a prefix for easy recognition (e.g. here is a UserId `usr_01JMVAW4T4320KJ3A7EJMCG8R0`).
2. Use the `[IdPrefix]` attribute with a short prefix (max 5 characters); strongly typed ULIDs are 26 characters long, and with the 5 characters prefix plus underscore, the total length is 32 characters, and database id columns are typically varchar(32).
3. Follow the naming convention of `[Entity]Id`.
4. Include the `[JsonConverter]` attribute for proper serialization.
5. Always override `ToString()` in the concrete class as record types will not inherit this method from the base class
6. Place the ID class in the same file as its corresponding aggregate or entity.
7. Use strongly typed IDs everywhere: API endpoints, request/response DTOs, commands, queries, and even in the frontend webapp
8. In rare cases, other ID types can be used for performance reasons (e.g., `TenantId` uses a strongly typed `long` because it's faster and used in almost every table).
9. Map strongly typed IDs in Entity Framework Core configurations.
10. `UserId` and `TenantId` are special cases as they need to be shared between self-contained systems, so they're defined in the shared kernel.

## Example 1 - UserId (Using default StronglyTypedUlid)

```csharp
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
```

## Example 2 - TenantId (Using StronglyTypedLongId with out prefix for performance)

```csharp
[PublicAPI]
[JsonConverter(typeof(StronglyTypedIdJsonConverter<long, TenantId>))]
public sealed record TenantId(long Value) : StronglyTypedLongId<TenantId>(Value)
{
    public override string ToString()
    {
        return Value.ToString();
    }
}
```

## Entity Framework Core Mapping

Strongly typed IDs must be properly mapped in Entity Framework Core configurations:

```csharp
public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.MapStronglyTypedUuid<User, UserId>(u => u.Id);
        builder.MapStronglyTypedLongId<User, TenantId>(u => u.TenantId);
    }
}
```

Use the appropriate mapping extension method based on the ID type:
- `MapStronglyTypedUuid` for ULIDs
- `MapStronglyTypedLongId` for long IDs
- `MapStronglyTypedGuid` for GUIDs
